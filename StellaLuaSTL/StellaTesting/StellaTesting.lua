local ColorPrint = require "Terminal.StellaColorPrint"

local StellaTesting = {}
-- Store test results
StellaTesting.results = {
    passed = 0,
    failed = 0,
    total = 0
}

-- Assertion Functions (Core Testing Building Blocks)
function StellaTesting.assertEqual(expected, actual, message)
    StellaTesting.results.total = StellaTesting.results.total + 1

    if expected == actual then
        StellaTesting.results.passed = StellaTesting.results.passed + 1
        ColorPrint.Green(string.format("[O] Test Passed: %s", message or ""))
    else
        StellaTesting.results.failed = StellaTesting.results.failed + 1
        ColorPrint.Red(string.format("[X] Test Failed: %s (Expected: %s, Got: %s)", message or "", expected, actual))
    end
end

-- Assertion Functions (Core Testing Building Blocks)
function StellaTesting.assertTrue(actual, message)
    StellaTesting.results.total = StellaTesting.results.total + 1

    if not actual then
        StellaTesting.results.failed = StellaTesting.results.failed + 1
        ColorPrint.Red(string.format("[X] Test Failed: %s (Expected: %s, Got: %s)", message or "", expected, actual))
    else
        StellaTesting.results.passed = StellaTesting.results.passed + 1
        ColorPrint.Green(string.format("[O] Test Passed: %s", message or ""))
    end
end

-- Function for running a test case
function StellaTesting.runTest(name, func)
    print(string.format("\n--- Running Test: %s ---", name))
    pcall(func) -- Run the test function, handling errors
end

-- Function to display test results summary
function StellaTesting.report()
    print("\n--- Test Summary ---")
    print(string.format("Total Tests: %d", StellaTesting.results.total))
    print(string.format("Passed: %d", StellaTesting.results.passed))
    print(string.format("Failed: %d", StellaTesting.results.failed))
end

return StellaTesting
