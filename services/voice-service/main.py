"""
BiSoyle Voice Service
Speaker Recognition Service using SpeechBrain ECAPA-TDNN
"""

import asyncio
import websockets
import json
import sys

class VoiceService:
    def __init__(self):
        self.is_initialized = False
        
    async def initialize(self):
        """Initialize speech recognition models"""
        print("Voice Service initializing...")
        # TODO: Initialize SpeechBrain model
        self.is_initialized = True
        print("Voice Service ready!")
        
    async def handle_websocket(self, websocket, path):
        """Handle WebSocket connection for real-time voice recognition"""
        print(f"New WebSocket connection: {path}")
        
        try:
            async for message in websocket:
                data = json.loads(message)
                
                if data.get('type') == 'audio':
                    # Process audio chunk
                    result = await self.process_audio(data.get('data'))
                    await websocket.send(json.dumps({
                        'type': 'result',
                        'best': result
                    }))
                    
        except websockets.exceptions.ConnectionClosed:
            print("WebSocket connection closed")
            
    async def process_audio(self, audio_data):
        """Process audio data and return speaker ID"""
        # TODO: Implement SpeechBrain inference
        return "Alice"
        
async def main():
    service = VoiceService()
    await service.initialize()
    
    server = await websockets.serve(
        service.handle_websocket,
        "localhost",
        8765
    )
    
    print("Voice Service running on ws://localhost:8765")
    await server.wait_closed()

if __name__ == "__main__":
    asyncio.run(main())






