"""
BiSoyle Voice Service
Speaker Recognition Service using SpeechBrain ECAPA-TDNN
"""

import asyncio
import websockets
import json
import sys
from aiohttp import web
import os
from pathlib import Path
import numpy as np
import torch
import torchaudio
from speechbrain.inference.speaker import SpeakerRecognition

class VoiceService:
    def __init__(self):
        self.is_initialized = False
        self.speakers = {}  # Store enrolled speakers: {name: audio_data}
        self.speakers_dir = Path("models/speakers")
        self.speakers_dir.mkdir(parents=True, exist_ok=True)
        self.last_result_time = 0.0  # Throttle results
        self.result_throttle_seconds = 1.0  # Send result max once per second
        self.model = None  # SpeechBrain model
        self.speaker_embeddings = {}  # Store speaker embeddings: {name: embedding}
        self.similarity_threshold = 0.5  # Minimum similarity to consider as match
        
    async def initialize(self):
        """Initialize speech recognition models"""
        print("Voice Service initializing...")
        try:
            # Load SpeechBrain ECAPA-TDNN model
            print("Loading SpeechBrain ECAPA-TDNN model...")
            self.model = SpeakerRecognition.from_hparams(
                source="speechbrain/spkrec-ecapa-voxceleb",
                savedir="models/speechbrain_ecapa",
                run_opts={"device": "cpu"}  # Use CPU for compatibility
            )
            print("SpeechBrain model loaded successfully!")
        except Exception as e:
            print(f"Warning: Could not load SpeechBrain model: {e}")
            print("Falling back to mock mode")
            self.model = None
            
        self.is_initialized = True
        self.load_speakers()
        print("Voice Service ready!")
    
    def load_speakers(self):
        """Load all enrolled speakers from disk"""
        if not self.speakers_dir.exists():
            return
        for speaker_file in self.speakers_dir.glob("*.wav"):
            name = speaker_file.stem
            self.speakers[name] = str(speaker_file)
            # Pre-compute embeddings if model is available
            if self.model is not None:
                try:
                    embedding = self.compute_embedding(speaker_file)
                    if embedding is not None:
                        self.speaker_embeddings[name] = embedding
                except Exception as e:
                    print(f"Warning: Could not compute embedding for {name}: {e}")
        print(f"Loaded {len(self.speakers)} speakers")
        
    async def handle_websocket(self, websocket, path):
        """Handle WebSocket connection for real-time voice recognition"""
        print(f"New WebSocket connection: {path}")
        import time
        
        try:
            async for message in websocket:
                # Handle both text (JSON) and binary (audio) messages
                if isinstance(message, bytes):
                    # Binary audio data - skip most chunks to avoid overload
                    import time
                    current_time = time.time()
                    if current_time - self.last_result_time < self.result_throttle_seconds:
                        continue  # Skip this chunk
                    
                    result = await self.process_audio(message)
                    if result:
                        if isinstance(result, dict) and result.get('error'):
                            # Error response
                            await websocket.send(json.dumps({
                                'type': 'error',
                                'message': result['error']
                            }))
                        elif len(result) > 0:
                            # Success response
                            self.last_result_time = current_time
                            await websocket.send(json.dumps({
                                'type': 'result',
                                'best': result[0][0],
                                'score': result[0][1],
                                'top': result
                            }))
                else:
                    # JSON message
                    data = json.loads(message)
                    
                    if data.get('type') == 'config':
                        print(f"Config received: sample rate = {data.get('sr')}")
                    elif data.get('type') == 'audio':
                        # Process audio chunk
                        result = await self.process_audio(data.get('data'))
                        if result:
                            if isinstance(result, dict) and result.get('error'):
                                # Error response
                                await websocket.send(json.dumps({
                                    'type': 'error',
                                    'message': result['error']
                                }))
                            elif len(result) > 0:
                                # Success response
                                await websocket.send(json.dumps({
                                    'type': 'result',
                                    'best': result[0][0],
                                    'score': result[0][1],
                                    'top': result
                                }))
                    
        except websockets.exceptions.ConnectionClosed:
            print("WebSocket connection closed")
        except Exception as e:
            print(f"WebSocket error: {e}")
            import traceback
            traceback.print_exc()
            
    def compute_embedding(self, audio_file):
        """Compute speaker embedding from audio file"""
        if self.model is None:
            return None
        try:
            # Load audio file
            signal, fs = torchaudio.load(str(audio_file))
            # Compute embedding
            embedding = self.model.encode_batch(signal)
            return embedding
        except Exception as e:
            print(f"Error computing embedding: {e}")
            return None
    
    def compute_embedding_from_bytes(self, audio_bytes):
        """Compute speaker embedding from raw audio bytes"""
        if self.model is None:
            return None
        try:
            # Convert bytes to audio signal
            import io
            audio_buffer = io.BytesIO(audio_bytes)
            signal, fs = torchaudio.load(audio_buffer)
            # Compute embedding
            embedding = self.model.encode_batch(signal)
            return embedding
        except Exception as e:
            print(f"Error computing embedding from bytes: {e}")
            return None
    
    def compare_embeddings(self, embedding1, embedding2):
        """Compare two embeddings using cosine similarity"""
        if embedding1 is None or embedding2 is None:
            return 0.0
        try:
            # Convert to numpy if needed
            if isinstance(embedding1, torch.Tensor):
                embedding1 = embedding1.squeeze().detach().numpy()
            if isinstance(embedding2, torch.Tensor):
                embedding2 = embedding2.squeeze().detach().numpy()
            
            # Normalize
            embedding1 = embedding1 / (np.linalg.norm(embedding1) + 1e-8)
            embedding2 = embedding2 / (np.linalg.norm(embedding2) + 1e-8)
            
            # Compute cosine similarity
            similarity = np.dot(embedding1, embedding2)
            return float(similarity)
        except Exception as e:
            print(f"Error comparing embeddings: {e}")
            return 0.0
    
    async def process_audio(self, audio_data):
        """Process audio data and return speaker ID"""
        if len(self.speakers) == 0 or len(self.speaker_embeddings) == 0:
            return {'error': 'no_enrollments'}
        
        # If model is not available, don't return anything (no mock)
        if self.model is None:
            return {'error': 'model_not_loaded'}
        
        # Compute embedding for input audio
        try:
            input_embedding = self.compute_embedding_from_bytes(audio_data)
            if input_embedding is None:
                return {'error': 'embedding_failed'}
            
            # Compare with all enrolled speakers
            results = []
            for name, ref_embedding in self.speaker_embeddings.items():
                similarity = self.compare_embeddings(input_embedding, ref_embedding)
                # Only include results above threshold
                if similarity >= self.similarity_threshold:
                    results.append([name, similarity])
            
            # Sort by similarity (descending)
            results.sort(key=lambda x: x[1], reverse=True)
            
            # Return None if no results above threshold (means unknown speaker, no error)
            return results if results else None
            
        except Exception as e:
            print(f"Error processing audio: {e}")
            return {'error': f'processing_failed: {str(e)}'}
        
# HTTP Handlers
async def handle_enroll(request):
    """Handle speaker enrollment via HTTP POST"""
    try:
        # Get form data
        reader = await request.multipart()
        name = None
        file_data = None
        
        async for field in reader:
            if field.name == 'name':
                name = await field.read()
                name = name.decode('utf-8')
            elif field.name == 'file':
                file_data = await field.read()
        
        if not name or not file_data:
            response = web.json_response({'ok': False, 'message': 'Name and file required'}, status=400)
            response.headers['Access-Control-Allow-Origin'] = '*'
            return response
        
        # Save speaker data
        speaker_file = service.speakers_dir / f"{name}.wav"
        speaker_file.write_bytes(file_data)
        
        service.speakers[name] = str(speaker_file)
        
        # Compute and store embedding
        if service.model is not None:
            try:
                embedding = service.compute_embedding(speaker_file)
                if embedding is not None:
                    service.speaker_embeddings[name] = embedding
                    print(f"Computed embedding for {name}")
            except Exception as e:
                print(f"Warning: Could not compute embedding for {name}: {e}")
        
        response = web.json_response({
            'ok': True,
            'message': f'Speaker {name} enrolled successfully'
        })
        response.headers['Access-Control-Allow-Origin'] = '*'
        return response
    except Exception as e:
        response = web.json_response({'ok': False, 'message': str(e)}, status=500)
        response.headers['Access-Control-Allow-Origin'] = '*'
        return response

async def handle_identify(request):
    """Handle speaker identification via HTTP POST"""
    try:
        reader = await request.multipart()
        file_data = None
        
        async for field in reader:
            if field.name == 'file':
                file_data = await field.read()
        
        if not file_data:
            response = web.json_response({'ok': False, 'message': 'File required'}, status=400)
            response.headers['Access-Control-Allow-Origin'] = '*'
            return response
        
        # Use real speaker identification
        if len(service.speakers) == 0:
            response = web.json_response({
                'ok': True,
                'best': None,
                'score': 0.0,
                'top': []
            })
            response.headers['Access-Control-Allow-Origin'] = '*'
            return response
        
        # Process using SpeechBrain or mock
        if service.model is None:
            # No model available
            response = web.json_response({
                'ok': True,
                'best': None,
                'score': 0.0,
                'top': []
            })
        else:
            # Real identification
            try:
                # Create a temporary file to process
                import tempfile
                with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as tmp_file:
                    tmp_file.write(file_data)
                    tmp_path = tmp_file.name
                
                embedding = service.compute_embedding(Path(tmp_path))
                os.unlink(tmp_path)
                
                if embedding is not None:
                    # Compare with all enrolled speakers
                    results = []
                    for name, ref_embedding in service.speaker_embeddings.items():
                        similarity = service.compare_embeddings(embedding, ref_embedding)
                        # Only include results above threshold
                        if similarity >= service.similarity_threshold:
                            results.append([name, similarity])
                    
                    # Sort by similarity (descending)
                    results.sort(key=lambda x: x[1], reverse=True)
                    
                    if results and len(results) > 0:
                        best_result = results[0]
                        response = web.json_response({
                            'ok': True,
                            'best': best_result[0],
                            'score': best_result[1],
                            'top': results[:5]  # Top 5 results
                        })
                    else:
                        response = web.json_response({
                            'ok': True,
                            'best': None,
                            'score': 0.0,
                            'top': []
                        })
                else:
                    response = web.json_response({
                        'ok': True,
                        'best': None,
                        'score': 0.0,
                        'top': []
                    })
            except Exception as e:
                print(f"Identification error: {e}")
                import traceback
                traceback.print_exc()
                response = web.json_response({
                    'ok': False,
                    'message': f'Identification failed: {str(e)}'
                }, status=500)
        
        response.headers['Access-Control-Allow-Origin'] = '*'
        return response
    except Exception as e:
        response = web.json_response({'ok': False, 'message': str(e)}, status=500)
        response.headers['Access-Control-Allow-Origin'] = '*'
        return response

async def handle_speakers_list(request):
    """Get list of enrolled speakers"""
    response = web.json_response({
        'ok': True,
        'speakers': list(service.speakers.keys())
    })
    response.headers['Access-Control-Allow-Origin'] = '*'
    return response

async def main():
    global service
    service = VoiceService()
    await service.initialize()
    
    # Add CORS middleware (MUST be defined before app)
    @web.middleware
    async def cors_handler(request, handler):
        # Handle preflight OPTIONS request
        if request.method == 'OPTIONS':
            return web.Response(headers={
                'Access-Control-Allow-Origin': '*',
                'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
                'Access-Control-Allow-Headers': 'Content-Type, Authorization',
                'Access-Control-Max-Age': '86400'
            })
        
        # Handle actual request
        response = await handler(request)
        response.headers['Access-Control-Allow-Origin'] = '*'
        response.headers['Access-Control-Allow-Methods'] = 'GET, POST, OPTIONS'
        response.headers['Access-Control-Allow-Headers'] = 'Content-Type, Authorization'
        return response
    
    # HTTP Server (for enroll/identify)
    app = web.Application(middlewares=[cors_handler])
    
    app.router.add_post('/enroll', handle_enroll)
    app.router.add_post('/identify', handle_identify)
    app.router.add_get('/api/speakers/list', handle_speakers_list)
    
    # Start HTTP and WebSocket servers on different ports
    host = "0.0.0.0"
    http_port = 8765
    ws_port = 8766
    
    # HTTP Server (for enroll/identify)
    http_runner = web.AppRunner(app)
    await http_runner.setup()
    http_site = web.TCPSite(http_runner, host, http_port)
    await http_site.start()
    
    # WebSocket Server (for live recognition)
    ws_server = await websockets.serve(
        service.handle_websocket,
        host,
        ws_port
    )
    
    print(f"Voice Service HTTP running on http://{host}:{http_port}")
    print(f"Voice Service WebSocket running on ws://{host}:{ws_port}")
    print(f"Voice Service initialized successfully!")
    
    # Keep the service running
    try:
        await asyncio.Future()  # Run forever
    finally:
        await http_runner.cleanup()
        ws_server.close()

if __name__ == "__main__":
    service = None
    asyncio.run(main())






