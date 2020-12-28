// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open FSharp.Interop.Excel
open FSharp.Interop.Excel.ExcelProvider
open System.Collections.Generic
open System.IO
open Manipulations
open FSharp.Configuration

// Let the type provider do it's work
type TestConfig = YamlConfig<"Config.yaml">

[<EntryPoint>]
let main argv =
    printfn "Use like:"
    let config = TestConfig()
    config
    let b=new a()
    
    let res=b.Data|>Seq.skip (config.MonthsRow-1)
    let dataIndexes= getRelevantColumns b.Data config.MonthsRow
    let res2= 
        takedata b.Data (config.DataStartRow-1) dataIndexes
        |>removeEmpties
        |>seperateNames
        |>createLists 1
       
    res2
    |>List.map (prepData>>makecsv)
    |>List.iteri(fun i x->  File.WriteAllText(i.ToString()+".csv",x) )      
    printfn "%A" res2
    0 // return an integer exit code