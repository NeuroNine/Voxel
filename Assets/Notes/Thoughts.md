# Some Design Thoughts

## Dynamic World

I want the world to continue to change and evolve as the player plays. Whether 
this be seasonal changes, environmental disasters, or some sort of other random
event. This should not be limited to the area that is currently loaded in memory
either. 

### Some Ideas

* General Changes like temperature/season could be a separate function run on 
top of the rest of world generation that could vary/shift over time; thus when 
loading/reloading a chunk, parts of it could change.
  * Might need to mark certain blocks as environmental and able to change 
 * When a chunk is deactivated, save the time. When the chunk is reloaded look
 at the time since it was last loaded and apply any relevant changes that might
 have occurred since it was last loaded.