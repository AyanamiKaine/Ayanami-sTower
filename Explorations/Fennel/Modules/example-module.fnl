;; Name of the module
(local MATH {})

;; A private constant within the module
(local PI 3.14159)

{
    :circle_area (fn [radius]
                    (* PI radius radius))

    :rectangle_area (fn [width height]
                   (* width height))
}

;; Used like: 
;; (local MATH (require :MATH)) ;; Must be in the same root folder

;; (local r 10)
;; (print "Area of a circle with radius" r ":" (SFPM.circle_area r))
;; Output: Area of a circle with radius  10  :   314.159

;; (print "Area of a 5x4 rectangle:" (SFPM.rectangle_area 5 4))
;; Output: Area of a 5x4 rectangle:   20