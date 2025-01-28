# BasicRTSSelection
 Implementation of a basic RTS style selection which creates bounds. 

Right now the solution makes a simple cubic bounds and transforms it to screen space which checks against the rectangle (drag select box).
It calculates the camera frustum every frame and does some extra checks which ignore objects away from the screen meant as an optimization technique etc..
