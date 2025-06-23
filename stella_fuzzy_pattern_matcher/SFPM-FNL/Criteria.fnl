;; criteria.fnl
;; A Fennel translation of the provided JavaScript Criteria class.

;; In Lua/Fennel, there is no direct equivalent to JavaScript's `Symbol`
;; or `Object.freeze`. The idiomatic way to create an "enum" is to use
;; a table with unique string values. By convention, this table is
;; treated as immutable.
(local Operator
  { :Equal "Equal"
    :GreaterThan "GreaterThan"
    :LessThan "LessThan"
    :GreaterThanOrEqual "GreaterThanOrEqual"
    :LessThanOrEqual "LessThanOrEqual"
    :NotEqual "NotEqual"
    :Predicate "Predicate" })


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


;; --- Criteria "Class" Definition ---

;; Define the prototype table for our Criteria "class".
(local Criteria {})
(tset Criteria :__index Criteria)

;; The "constructor" function.
(fn Criteria.new [factName expectedValue operator]
  (when (and (= operator Operator.Predicate) (not= (type expectedValue) "function"))
    (error "For the Predicate operator, expectedValue must be a function."))

  ;; `let` creates local bindings for our new instance's properties.
  (let [instance { :factName factName
                   :expectedValue expectedValue
                   :operator operator
                   ;; Set the predicate only if the operator matches.
                   :predicate (when (= operator Operator.Predicate) expectedValue) }]
    ;; Set the instance's metatable to the prototype to enable method calls.
    (setmetatable instance Criteria)
    instance))

;; The `evaluate` method. We define it on the `Criteria` table using dot
;; notation and explicitly add `self` as the first argument.
(fn Criteria.evaluate [self facts]
  ;; Assumes `facts` is a table with a `:getFact` method.
  (let [actualValue (: facts :getFact self.factName)]
    ;; In Lua, `nil` is the closest equivalent to JavaScript's `undefined`
    ;; for a value that doesn't exist.
    (if (= actualValue nil)
        false
        ;; If the operator is Predicate, call the stored function.
        (if (= self.operator Operator.Predicate)
            (self.predicate actualValue)
            ;; `cond` is a  macro that is a clean replacement
            ;; for a `switch` statement or a long `if/elseif` chain.
            ;; It evaluates each condition in order and returns the
            ;; value of the expression for the first one that is true.
            (cond
              [(= self.operator Operator.Equal) (= actualValue self.expectedValue)]
              [(= self.operator Operator.NotEqual) (not= actualValue self.expectedValue)]
              [(= self.operator Operator.GreaterThan) (> actualValue self.expectedValue)]
              [(= self.operator Operator.LessThan) (< actualValue self.expectedValue)]
              [(= self.operator Operator.GreaterThanOrEqual) (>= actualValue self.expectedValue)]
              [(= self.operator Operator.LessThanOrEqual) (<= actualValue self.expectedValue)]
              ;; Default case if no operator matches.
              [true false])))))

;; This is a module, so we return a table containing the public parts
;; that can be used by other files that `require` this one.
{ :Operator Operator
  :Criteria Criteria }
