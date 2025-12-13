Manage balance patches - save, load, and version control configurations.

Usage: /patch [action] [args...]

SAVE ACTION:
  /patch save [name] [description]
  - name: Patch name (e.g., 'enhanced-enemies-v1')
  - description: What changed in this patch

  This creates a versioned snapshot of current balance configuration that can be:
  - Shared with team members
  - Reverted to if needed
  - Compared against to see what changed

  Example: /patch save enhanced-enemies-v1 "Increased enemy variance with comprehensive overrides"

LIST ACTION:
  /patch list
  - Lists all saved balance patches with metadata

LOAD ACTION:
  /patch load [name]
  - name: Patch name to restore
  - Applies a previously saved patch
  - Verifies configuration was loaded correctly

  Example: /patch load enhanced-enemies-v1

INFO ACTION:
  /patch info [name]
  - Shows detailed information about a patch
  - When it was created, who created it, what changed

  Example: /patch info enhanced-enemies-v1

CONFIG MANAGER AGENT:
This command launches the Config Manager Agent which:
1. Tracks all configuration changes
2. Manages patch versioning
3. Creates backups before major changes
4. Maintains change history
5. Enables easy rollback
