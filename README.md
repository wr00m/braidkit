# braidkit
Command-line tool for manipulating and modifying the puzzle-platform game Braid, designed for exploration, experimentation and theorycrafting.

**Note to users:** This tool modifies game memory at runtime and uses DLL injection. If you use it in a way that violates any End User License Agreement (EULA), Terms of Service (ToS), or similar policies, you are solely responsible for the consequences. Please use it responsibly.

**Note to speedrunners:** The game must be restarted after using this tool to ensure an unmodified game state before performing any competitive speedruns, except for the `il-timer` command which is allowed for [individual level speedruns](http://bit.ly/BraidIL).

##
```
braidkit camera-lock                         // Lock camera at current position
braidkit camera-lock 10 20                   // Lock camera at x=10 y=20
braidkit camera-lock toggle                  // Toggle camera lock/unlock
braidkit camera-lock unlock                  // Unlock camera
braidkit camera-zoom 0.5                     // Zoom out camera
braidkit camera-zoom reset                   // Reset camera to default zoom
braidkit tim-position 10 20                  // Move Tim to x=10 y=20
braidkit tim-position 10 20 -r               // Move Tim by x=10 y=20 relative to current position
braidkit tim-velocity 100 200                // Set Tim's velocity to x=100 y=200
braidkit tim-speed 2.0                       // Set Tim's movement speed to 200 %
braidkit tim-jump 1.5                        // Set Tim's jump speed to 150 %
braidkit entity-flag monstar greenglow false // Remove green glow from all goombas
braidkit entity-flag mimic nogravity true    // Disable gravity for all rabbits
braidkit entity-flag guy hidden true         // Make Tim invisible
braidkit bg-full-speed                       // Toggle game running at full speed in background
braidkit il-timer                            // Print level complete times on door entry
braidkit il-timer --live                     // Additionally print live timer
braidkit il-timer --reset-pieces             // Additionally reset all puzzle pieces on level entry
braidkit reset-pieces                        // Resets all puzzle pieces on current save
braidkit debug-info                          // Toggle in-game debug info
braidkit render-overlay                      // Render in-game debug overlay, e.g., colliders
braidkit -h                                  // Show help
```

![Screenshot](braidkit_screenshot.jpg)