local StellaSTL = require "StellaSTL"
local StellaTesting = StellaSTL.StellaTesting

StellaTesting.runTest("Basic Addition", function()
    StellaTesting.assertEqual(5, 2 + 3, "Simple addition should work")
    StellaTesting.assertEqual(10, 2 * 5, "Multiplication test")
end)

-- Add more tests...

StellaTesting.report() -- Show the results