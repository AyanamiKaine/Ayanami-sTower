(local SFPM {})

;; A private constant within the module
(local PI 3.14159)

{:circle_area (fn [radius]
                (* PI radius radius))

 :rectangle_area (fn [width height]
                   (* width height))}
