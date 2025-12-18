#!/usr/bin/env python3
"""
Interactive MCP client to play DungeonFighter through the MCP server
"""

import json
import subprocess
import sys
import time

class MCPGamePlayer:
    def __init__(self):
        self.process = None
        self.request_id = 1
        self.game_state = None

    def start_server(self):
        """Start the MCP server"""
        print("Starting MCP server...")
        self.process = subprocess.Popen(
            [
                "dotnet", "run",
                "--project", "Code/Code.csproj",
                "--", "MCP"
            ],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            text=True,
            bufsize=1
        )
        time.sleep(2)  # Give server time to start
        print("MCP server started!")

    def send_request(self, tool_name, arguments):
        """Send a request to the MCP server"""
        request = {
            "jsonrpc": "2.0",
            "id": self.request_id,
            "method": "tools/call",
            "params": {
                "name": tool_name,
                "arguments": arguments
            }
        }
        self.request_id += 1

        print(f"\n[Sending] {tool_name}")
        request_str = json.dumps(request)

        try:
            self.process.stdin.write(request_str + "\n")
            self.process.stdin.flush()

            # Read response
            response_line = self.process.stdout.readline()
            if response_line:
                response = json.loads(response_line)
                return response
        except Exception as e:
            print(f"Error: {e}")
            return None

    def start_game(self):
        """Start a new game"""
        return self.send_request("start_new_game", {})

    def get_game_state(self):
        """Get current game state"""
        response = self.send_request("get_game_state", {})
        if response and "result" in response:
            self.game_state = response["result"]
            return self.game_state
        return None

    def get_available_actions(self):
        """Get available actions"""
        return self.send_request("get_available_actions", {})

    def handle_input(self, action):
        """Send input to the game"""
        return self.send_request("handle_input", {"input": action})

    def play(self):
        """Main game loop"""
        self.start_server()

        # Start new game
        print("\n=== Starting New Game ===")
        response = self.start_game()
        if response:
            print(json.dumps(response, indent=2))

        # Get initial state
        print("\n=== Getting Initial Game State ===")
        state = self.get_game_state()
        if state:
            print(json.dumps(state, indent=2)[:500])  # Print first 500 chars

        # Get available actions
        print("\n=== Available Actions ===")
        actions = self.get_available_actions()
        if actions:
            print(json.dumps(actions, indent=2))

        print("\n=== Game Ready to Play ===")

        # Interactive loop
        while True:
            try:
                action = input("\nEnter action (or 'quit' to exit): ").strip()
                if action.lower() == 'quit':
                    break

                response = self.handle_input(action)
                print(f"Response: {json.dumps(response, indent=2)[:500]}")

                # Get updated state
                state = self.get_game_state()
                if state:
                    print(f"State: {json.dumps(state, indent=2)[:500]}")

            except KeyboardInterrupt:
                break

        print("\nGame ended. Shutting down...")
        if self.process:
            self.process.terminate()
            self.process.wait()

if __name__ == "__main__":
    player = MCPGamePlayer()
    player.play()
