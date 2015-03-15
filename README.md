UI Layout Engine
By Jeff Gaston

The VisiPlacer was created to further automate the calculation of the locations of views onscreen in an application.

Many frameworks will ask each view how big it wants to be, and allow the view to set its own size. Then, when the views render, some will be cropped or otherwise distorted if necessary. This makes sense and it's more intuitive to understand. The VisiPlacer creates a fuller API through which layouts can communicate: each layout is instead responsible with telling what its score would be if it were required to fit into a certain size, and this allows collection views to adjust their boundaries to maximize the overall score. Typically, a view creates a significant score penalty if it must be cropped, but only a small score penalty if its content is shrunken or off-center.

The VisiPlacer supports lots of unusual and interesting behaviors, all based on the idea that the entire visual tree may be recalculated at runtime. Some helpful behaviors that it supports are to resize when screen dimensions change, or to shrink text as the user types, or to hide or move less-important items as the screen becomes crowded.

See https://github.com/mathjeff/ActivityRecommender-WPhone for a project that makes use of this one.