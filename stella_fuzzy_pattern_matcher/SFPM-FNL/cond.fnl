(macro cond [& clauses]
    (if (<= (# clauses) 0)
        nil
        (let [condition (. clauses 1)
              then-branch (. clauses 2)]
            (if (= condition :else)
                then-branch
                `(if ,condition
                    ,then-branch
                    (cond ,(unpack clauses 3))   
                )
            )
        )
    )
)


;; This is a cond with 0 clauses and so triggers the first
;; part of the macro condition (if (<= (# clauses) 0) nil) and so returns nil
(cond)

(local first-value 1)

(cond 
  (< first-value 5)   (print "first-value is less than 5")
  (> first-value 10 ) (print "first-value is greater than 10")
  :else (print "first-value is between 5 and 10 (inclusive)"))

(local second-value 7)
(cond 
  (< second-value 5)   (print "second-value is less than 5")
  (> second-value 10 ) (print "second-value is greater than 10")
  :else (print "second-value is between 5 and 10 (inclusive)"))

(local third-value 15)
(cond 
  (< third-value 5)   (print "third-value is less than 5")
  (> third-value 10 ) (print "third-value is greater than 10")
  :else (print "third-value is between 5 and 10 (inclusive)"))


(fn my-first-test-function [a b]
  "This is a test function."
  (+ a b))

(local my-second-function (fn [x]
  (* x x)))