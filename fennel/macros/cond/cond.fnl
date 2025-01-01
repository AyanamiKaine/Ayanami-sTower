(fn cond [& clauses]
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

{: cond}