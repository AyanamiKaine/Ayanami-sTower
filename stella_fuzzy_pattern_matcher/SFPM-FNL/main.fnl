(local SFPM (require :SFPM))

(local r 10)
(print "Area of a circle with radius" r ":" (SFPM.circle_area r))
;; Output: Area of a circle with radius  10  :   314.159

(print "Area of a 5x4 rectangle:" (SFPM.rectangle_area 5 4))
;; Output: Area of a 5x4 rectangle:   20

;; The 'PI' variable is private to the module and not accessible here.
;; (print SFPM.PI) -- This result in an error