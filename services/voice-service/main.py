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
from speechbrain.pretrained import SpeakerRecognition
from pydub import AudioSegment
import io

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
            # Windows'ta symlink sorununu önlemek için environment variable kullan
            import os
            os.environ['HF_HUB_DOWNLOAD_STRATEGY'] = 'copy'  # Symlink yerine copy kullan
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
        
        # Group files by speaker name (handle {name}_{idx}.mp3 format)
        speaker_files_dict = {}  # {speaker_name: [file1, file2, ...]}
        
        # Load MP3 files
        for speaker_file in self.speakers_dir.glob("*.mp3"):
            stem = speaker_file.stem
            # Check if it's in format {name}_{number}
            if '_' in stem:
                # Try to extract base name (remove trailing _number)
                parts = stem.rsplit('_', 1)
                if len(parts) == 2 and parts[1].isdigit():
                    # It's {name}_{idx} format
                    base_name = parts[0]
                    if base_name not in speaker_files_dict:
                        speaker_files_dict[base_name] = []
                    speaker_files_dict[base_name].append(speaker_file)
                else:
                    # Regular format, use as is
                    if stem not in speaker_files_dict:
                        speaker_files_dict[stem] = []
                    speaker_files_dict[stem].append(speaker_file)
            else:
                # No underscore, use as is
                if stem not in speaker_files_dict:
                    speaker_files_dict[stem] = []
                speaker_files_dict[stem].append(speaker_file)
        
        # Load WAV files (same logic)
        for speaker_file in self.speakers_dir.glob("*.wav"):
            stem = speaker_file.stem
            if '_' in stem:
                parts = stem.rsplit('_', 1)
                if len(parts) == 2 and parts[1].isdigit():
                    base_name = parts[0]
                    if base_name not in speaker_files_dict:
                        speaker_files_dict[base_name] = []
                    speaker_files_dict[base_name].append(speaker_file)
                else:
                    if stem not in speaker_files_dict:
                        speaker_files_dict[stem] = []
                    speaker_files_dict[stem].append(speaker_file)
            else:
                if stem not in speaker_files_dict:
                    speaker_files_dict[stem] = []
                speaker_files_dict[stem].append(speaker_file)
        
        # Process each speaker's files
        for speaker_name, files in speaker_files_dict.items():
            # Store first file as reference
            self.speakers[speaker_name] = str(files[0])
            
            # Compute embeddings for all files and average them
            if self.model is not None:
                embeddings = []
                for speaker_file in files:
                    try:
                        embedding = self.compute_embedding(speaker_file)
                        if embedding is not None:
                            embeddings.append(embedding)
                    except Exception as e:
                        print(f"Warning: Could not compute embedding for {speaker_name} file {speaker_file.name}: {e}")
                
                # Average embeddings if multiple files
                if len(embeddings) > 0:
                    try:
                        if len(embeddings) == 1:
                            self.speaker_embeddings[speaker_name] = embeddings[0]
                        else:
                            # Average all embeddings
                            embeddings_np = []
                            for emb in embeddings:
                                if isinstance(emb, torch.Tensor):
                                    emb = emb.squeeze().detach().numpy()
                                embeddings_np.append(emb)
                            
                            embeddings_array = np.stack(embeddings_np)
                            avg_embedding = np.mean(embeddings_array, axis=0)
                            avg_embedding = torch.tensor(avg_embedding).unsqueeze(0)
                            self.speaker_embeddings[speaker_name] = avg_embedding
                        
                        print(f"Loaded speaker '{speaker_name}' with {len(files)} file(s)")
                    except Exception as e:
                        print(f"Warning: Could not average embeddings for {speaker_name}: {e}")
                        if embeddings:
                            self.speaker_embeddings[speaker_name] = embeddings[0]
        
        print(f"Loaded {len(self.speakers)} speakers with {len(self.speaker_embeddings)} embeddings")
        
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
    """Handle speaker enrollment via HTTP POST - supports multiple files for better accuracy"""
    try:
        # Get form data
        reader = await request.multipart()
        name = None
        file_data_list = []  # Support multiple files
        
        async for field in reader:
            field_name = field.name
            print(f"Received field: name={field_name}, filename={getattr(field, 'filename', None)}")
            
            if field_name == 'name':
                name = await field.read()
                name = name.decode('utf-8')
                print(f"Name extracted: {name}")
            elif field_name == 'file' or field_name == 'files[]' or field_name.startswith('file'):
                file_data = await field.read()
                if file_data and len(file_data) > 0:
                    file_data_list.append(file_data)
                    print(f"File added: {len(file_data)} bytes")
        
        print(f"Total files received: {len(file_data_list)}, name: {name}")
        
        if not name or len(file_data_list) == 0:
            error_msg = f'Name and at least one file required. Got name={name is not None}, files={len(file_data_list)}'
            print(error_msg)
            response = web.json_response({'ok': False, 'message': error_msg}, status=400)
            response.headers['Access-Control-Allow-Origin'] = '*'
            return response
        
        # Ensure speakers directory exists
        service.speakers_dir.mkdir(parents=True, exist_ok=True)
        print(f"Speakers directory: {service.speakers_dir.absolute()}")
        
        # Process all files and compute embeddings
        embeddings = []
        saved_files = []
        
        for idx, file_data in enumerate(file_data_list):
            try:
                print(f"Processing file {idx+1}/{len(file_data_list)} for {name}, size: {len(file_data)} bytes")
                
                # Load audio from bytes (supports various formats)
                audio_buffer = io.BytesIO(file_data)
                
                # Try to detect format first
                try:
                    audio = AudioSegment.from_file(audio_buffer)
                    print(f"Audio loaded: {len(audio)}ms, {audio.frame_rate}Hz")
                except Exception as e:
                    print(f"Error loading audio from buffer: {e}")
                    # Try with explicit format
                    audio_buffer.seek(0)
                    try:
                        audio = AudioSegment.from_file(audio_buffer, format="webm")
                    except:
                        audio_buffer.seek(0)
                        audio = AudioSegment.from_file(audio_buffer, format="wav")
                
                # Save each file separately (for backup/reference)
                # Clean name for filename (remove special characters)
                safe_name = "".join(c for c in name if c.isalnum() or c in (' ', '-', '_')).strip()
                safe_name = safe_name.replace(' ', '_')
                
                speaker_file = service.speakers_dir / f"{safe_name}_{idx+1}.mp3"
                
                try:
                    # Ensure parent directory exists
                    speaker_file.parent.mkdir(parents=True, exist_ok=True)
                    audio.export(str(speaker_file), format="mp3", bitrate="128k")
                    print(f"✓ Saved as MP3: {speaker_file.absolute()}")
                    if not speaker_file.exists():
                        raise Exception(f"File was not created: {speaker_file}")
                except Exception as e:
                    print(f"MP3 export failed: {e}, trying WAV...")
                    # Fallback to WAV if MP3 fails
                    speaker_file = service.speakers_dir / f"{safe_name}_{idx+1}.wav"
                    speaker_file.parent.mkdir(parents=True, exist_ok=True)
                    audio.export(str(speaker_file), format="wav")
                    print(f"✓ Saved as WAV: {speaker_file.absolute()}")
                    if not speaker_file.exists():
                        raise Exception(f"File was not created: {speaker_file}")
                
                saved_files.append(speaker_file)
                print(f"✓ Successfully saved speaker {name} file {idx+1} as {speaker_file.suffix}: {speaker_file.absolute()}")
                
                # Compute embedding for this file
                if service.model is not None:
                    try:
                        embedding = service.compute_embedding(speaker_file)
                        if embedding is not None:
                            embeddings.append(embedding)
                            print(f"✓ Computed embedding {idx+1}/{len(file_data_list)} for {name}")
                        else:
                            print(f"⚠ Warning: Embedding is None for file {idx+1}")
                    except Exception as e:
                        print(f"⚠ Warning: Could not compute embedding for {name} file {idx+1}: {e}")
                        import traceback
                        traceback.print_exc()
                else:
                    print(f"⚠ Model not available, skipping embedding computation")
                        
            except Exception as e:
                print(f"❌ Error processing file {idx+1} for {name}: {e}")
                import traceback
                traceback.print_exc()
                # Don't continue if critical error, but log it
                # continue
        
        # Store the main file reference (first file or combined)
        if saved_files:
            service.speakers[name] = str(saved_files[0])
            print(f"✓ Registered speaker '{name}' with {len(saved_files)} file(s)")
        else:
            print(f"❌ No files were saved for {name}!")
            response = web.json_response({
                'ok': False, 
                'message': f'Failed to save files for {name}. Check server logs for details.'
            }, status=500)
            response.headers['Access-Control-Allow-Origin'] = '*'
            return response
        
        # Compute average embedding from all files (more reliable)
        if service.model is not None and len(embeddings) > 0:
            try:
                # Average all embeddings
                if len(embeddings) == 1:
                    avg_embedding = embeddings[0]
                else:
                    # Convert to numpy and average
                    embeddings_np = []
                    for emb in embeddings:
                        if isinstance(emb, torch.Tensor):
                            emb = emb.squeeze().detach().numpy()
                        embeddings_np.append(emb)
                    
                    # Stack and average
                    embeddings_array = np.stack(embeddings_np)
                    avg_embedding = np.mean(embeddings_array, axis=0)
                    # Convert back to tensor if needed
                    avg_embedding = torch.tensor(avg_embedding).unsqueeze(0)
                
                service.speaker_embeddings[name] = avg_embedding
                print(f"Stored average embedding for {name} from {len(embeddings)} files")
            except Exception as e:
                print(f"Warning: Could not compute average embedding for {name}: {e}")
                # Fallback: use first embedding
                if embeddings:
                    service.speaker_embeddings[name] = embeddings[0]
        
        response = web.json_response({
            'ok': True,
            'message': f'Speaker {name} enrolled successfully with {len(file_data_list)} file(s)'
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
        print("=== Identify request received ===")
        reader = await request.multipart()
        file_data = None
        
        async for field in reader:
            if field.name == 'file':
                file_data = await field.read()
                print(f"File received: {len(file_data)} bytes")
        
        if not file_data:
            print("ERROR: No file data received")
            response = web.json_response({'ok': False, 'message': 'File required'}, status=400)
            response.headers['Access-Control-Allow-Origin'] = '*'
            return response
        
        # Use real speaker identification
        print(f"Total speakers in system: {len(service.speakers)}")
        print(f"Total embeddings available: {len(service.speaker_embeddings)}")
        
        if len(service.speakers) == 0 or len(service.speaker_embeddings) == 0:
            print("WARNING: No speakers or embeddings available")
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
            print("ERROR: Model is not available!")
            response = web.json_response({
                'ok': True,
                'best': None,
                'score': 0.0,
                'top': []
            })
            response.headers['Access-Control-Allow-Origin'] = '*'
            return response
        else:
            print("Model is available, processing...")
            # Real identification
            try:
                # Convert audio to WAV format first (for better compatibility)
                import tempfile
                audio_buffer = io.BytesIO(file_data)
                
                try:
                    # Try to load audio from bytes
                    audio = AudioSegment.from_file(audio_buffer)
                    print(f"Loaded audio: {len(audio)}ms, {audio.frame_rate}Hz")
                except Exception as e:
                    print(f"Error loading audio, trying explicit formats: {e}")
                    # Try with explicit formats
                    audio_buffer.seek(0)
                    try:
                        audio = AudioSegment.from_file(audio_buffer, format="webm")
                    except:
                        audio_buffer.seek(0)
                        try:
                            audio = AudioSegment.from_file(audio_buffer, format="mp3")
                        except:
                            audio_buffer.seek(0)
                            audio = AudioSegment.from_file(audio_buffer, format="wav")
                
                # Export to WAV for processing
                with tempfile.NamedTemporaryFile(suffix='.wav', delete=False) as tmp_file:
                    audio.export(tmp_file.name, format="wav")
                    tmp_path = tmp_file.name
                
                print(f"Processing audio file: {tmp_path}")
                embedding = service.compute_embedding(Path(tmp_path))
                os.unlink(tmp_path)
                
                if embedding is not None:
                    # Compare with all enrolled speakers
                    results = []
                    for name, ref_embedding in service.speaker_embeddings.items():
                        similarity = service.compare_embeddings(embedding, ref_embedding)
                        # Include all results (not just above threshold) for display
                        results.append([name, float(similarity)])
                    
                    # Sort by similarity (descending)
                    results.sort(key=lambda x: x[1], reverse=True)
                    
                    print(f"Identification results: {len(results)} speakers compared")
                    for name, score in results[:5]:
                        print(f"  - {name}: {score:.3f}")
                    
                    if results and len(results) > 0:
                        best_result = results[0]
                        # Only set 'best' if above threshold
                        best_name = best_result[0] if best_result[1] >= service.similarity_threshold else None
                        best_score = best_result[1] if best_result[1] >= service.similarity_threshold else best_result[1]
                        
                        response = web.json_response({
                            'ok': True,
                            'best': best_name,
                            'score': best_score,
                            'top': results[:10]  # Top 10 results (all speakers)
                        })
                    else:
                        response = web.json_response({
                            'ok': True,
                            'best': None,
                            'score': 0.0,
                            'top': []
                        })
                else:
                    print("ERROR: Embedding is None after computation")
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
        
        if 'response' not in locals():
            print("ERROR: No response created!")
            response = web.json_response({
                'ok': False,
                'message': 'Internal error: No response generated'
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
    http_port = int(os.getenv("HTTP_PORT", "8765"))
    ws_port = int(os.getenv("WS_PORT", "8766"))
    
    # Port kontrolü - başka bir servis kullanıyorsa farklı port dene
    import socket
    def is_port_in_use(port):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
            return s.connect_ex((host, port)) == 0
    
    # Port çakışması kontrolü
    if is_port_in_use(http_port):
        print(f"Warning: Port {http_port} is already in use, trying alternative port...")
        http_port = 8767
    if is_port_in_use(ws_port):
        print(f"Warning: Port {ws_port} is already in use, trying alternative port...")
        ws_port = 8768
    
    # HTTP Server (for enroll/identify)
    http_runner = web.AppRunner(app)
    await http_runner.setup()
    try:
        http_site = web.TCPSite(http_runner, host, http_port)
        await http_site.start()
    except OSError as e:
        if hasattr(e, 'winerror') and e.winerror == 10048:  # Port already in use (Windows)
            print(f"Error: Port {http_port} is already in use. Trying alternative...")
            http_port = 8767
            http_site = web.TCPSite(http_runner, host, http_port)
            await http_site.start()
        else:
            raise
    
    # WebSocket Server (for live recognition)
    try:
        ws_server = await websockets.serve(
            service.handle_websocket,
            host,
            ws_port
        )
    except OSError as e:
        if hasattr(e, 'winerror') and e.winerror == 10048:  # Port already in use
            print(f"Error: Port {ws_port} is already in use. Trying alternative...")
            ws_port = 8768
            ws_server = await websockets.serve(
                service.handle_websocket,
                host,
                ws_port
            )
        else:
            raise
    
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






