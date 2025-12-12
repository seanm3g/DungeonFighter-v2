#!/usr/bin/env python3
"""
Simple MCP client to test the DungeonFighter simulation tools
Sends commands to the MCP server running on stdio
"""

import json
import sys

# MCP request format for run_battle_simulation
request = {
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
        "name": "run_battle_simulation",
        "arguments": {
            "battlesPerCombination": 50,
            "playerLevel": 1,
            "enemyLevel": 1
        }
    }
}

print(json.dumps(request))
sys.stdout.flush()
