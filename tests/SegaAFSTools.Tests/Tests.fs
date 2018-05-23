namespace SegaAFSTools.Tests

open Expecto
open FsCheck

module Tests =
    let config10k = { FsCheckConfig.defaultConfig with maxTest = 10000}

    [<Tests>]
    let testSimpleTests =
        testList "SegaAFSTools" []

