namespace SegaAFSTools.Tests

open Expecto

module RunTests =

    [<EntryPoint>]
    let main args =

        Tests.runTestsWithArgs defaultConfig args tests.testSimpleTests |> ignore

        0

