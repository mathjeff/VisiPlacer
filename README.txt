VisiPlacer is a layout engine that enables layouts to ask their children how happy they would be with certain dimensions, and to then allocate space accordingly.

This can, for example, make it easy to resize text boxes while the user types.
This can also make it easy to:
  at larger resolutions, display a single menu with many buttons
  at smaller resolutions, display one menu at a time from a set of nested menus, with each menu having few buttons

Each layout is expected to inherit from LayoutChoice_Set and to implement:

GetBestLayout(LayoutQuery query);

Each LayoutQuery provides a set of constraints: maximum display width, maximum display height, and minimum score.
Each LayoutQuery requests that the LayoutChoice_Set return the dimensions optimizing a specific coordinate: score, width, or height, depending on the class of LayoutQuery:

MaxScore_LayoutQuery
MinWidth_LayoutQuery
MinHeight_LayoutQuery

The dimensions of the matching layout must be contained in a SpecificLayout and returned from the GetBestLayout method.
Each LayoutChoice_Set must support and correctly answer each of the three subclasses of LayoutQuery.

It is required that increasing the width or height given to a SpecificLayout must not decrease its score.
However, it is permissible for an increase in width or height to cause no change in score.
It is also permissible for score to decrease due to the combination of an increase in one dimension but a decrease in the other.
The reason for this requirement is to make it easier for containing layouts to reason about how much space to give to various child view.

If any aspects of a LayoutChoice_Set change that will affect the validity of its dimensions, it should call AnnounceChange to inform the layout engine.
For example, a TextLayout may call AnnounceChange when its text changes.

The root of the layout hierarchy is the ViewManager, which initiates the layout process and attaches the results to the view hierarchy.

For example usage, see https://github.com/mathjeff/ActivityRecommender and look for "ViewManager".