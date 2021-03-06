// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open FSharp.Interop.Excel
open FSharp.Interop.Excel.ExcelProvider
open System.Collections.Generic
open System.IO
open Manipulations
open FSharp.Configuration
open FSharp.Data.Dapper
open FSharp.Data
// Let the type provider do it's work
type TestConfig = YamlConfig<"Config.yaml">
type ClientData=string * list<option<string>>
type FilesNamesMonthsPayments=list<list<ClientData>>
let processFile  (config:TestConfig) (csvData:string option list list)=
   // let res=csvData|>Seq.skip (config.MonthsRow-1)
    let dataIndexes= getRelevantColumns csvData config.MonthsRow
    printfn "keeping columns: %A" dataIndexes
    let dataRange= csvData|>List.skip(config.DataStartRow-1)
    let res2= 
        dataRange 
        |>selectColumns dataIndexes
        |>removeEmptyRows
        |>removeLastColumn
        |>seperateNames

       
    res2

//contains the indexs of relevant items
type ClientRowTest={Greater: int list ; Same: int list}
type ComparisonResult=
    |Combine of int list
    |Leave of int
    |Add of int
    //deign: build a list of the 

let combineYear monthsPerFile (previousYears,dict) yearsData =
    let preMonths =List.init (monthsPerFile*previousYears) (fun _->None)
    //adding each customers data for the year to the previous years data. Either concatenating or adding a new entry
    let out=
        yearsData|>List.fold
            (fun (map:Map<string,string option list>) (name,data) ->
                map.Change 
                    (name,
                    //Either adds a new entry with empty data for previous months or adds the new data to the previous data.
                    //These two functions are identical. one is just a little bit more fancy
                    Option.map ((@)data) >> Option.orElse (Some (preMonths@data)) 
                    (* (fun key -> 
                        match key with
                        |Some(x)->  (x@data)
                        |None->  data
                        |>Some)  *)
                    )
             ) 
            (dict)   
    (previousYears+1,out)
let checkCorrectNumberOfMonths monthsPerFile (client:ClientData)=
    if (client|>snd).Length<> monthsPerFile then
        printfn "ERROR:====="
        printfn "The configured 'MonthsPerFile' (%i) does not match number of months found (%i) for client (%s)" monthsPerFile ((client|>snd).Length)( client|>fst)
        printfn "======="
let combineData2 monthsPerFile (dataList:FilesNamesMonthsPayments) =
    let head::rest=dataList
    checkCorrectNumberOfMonths monthsPerFile head.Head
    let masterDic= Map.ofList(head);

    let res=
        rest
        //we fold it with the dictionary so we acccumulate the chagnes made by each years data
        //we allso include how many months have been before so we can prepend 12 NONEs per privous year  for those to any new data .
        |>List.fold (combineYear monthsPerFile) (1,masterDic) 

    let requiredLength=dataList.Length*monthsPerFile 

    res
    |>snd
    //We append Nones so that all clinets have a payment entry for every month
    |>Map.map(fun _ item ->
        let additionalMonths=requiredLength- item.Length
        if additionalMonths>0 then
            item@(List.init (additionalMonths) (fun x-> None))
        else item
        )
    |>Map.toList
        


    ///horrible old way that is terrible and stupid and complicated
(* let combineData (dataList:FilesNamesMonthsPayments)=
    let heads=
        dataList
        |>List.map(fun year-> year.Head)
   
    let indexedHeads=heads|>List.indexed 
    let list=   
        indexedHeads
        |>List.map(fun (i,clientData)-> 
            let clientName,_= clientData
            let results=
                indexedHeads|> List.fold 
                    (fun results (index,(testName,_))-> 
                        match clientName with
                        |x when x>testName-> {results with Greater= index::results.Greater}
                        |x when x= testName->{results with Same=index::results.Same} 
                        |_-> results)
                    {Greater=[];Same=[]}  
            let action=
                match results with
                |{Same=same}       when same<>[]   ->Combine ((i::same)|>List.sort)
                |{Greater=greater} when greater<>[]-> Leave i
                |_-> Add i
            action
            )
    let doActions (actions: ComparisonResult list) (heads:list<string * list<option<string>>>) =
        match actions with
        |action::tail->
            match action with 
            |Combine combinations->
                let a=combinations|>List.fold(fun state i -> state@(heads.[i]|>snd)) []
            //impliment cases for add. which just adds the unchanged clientdata to the output
            //and leave which does nothing. 
            //i then need to remove duplicates of combinations
            //then  i need to compute which clientdatas should be removed and which should be left. 
            |->
    let a=doActions list heads
    0 *)
    //combine lists
    //sort lists
    //add items from the source list 
    //
[<EntryPoint>]
let main argv =
    printfn "Use like:"
    
    let config =TestConfig();
    // I would think it would only have to be one to take into account 0 indexing but for some reason we loose the first line of th csv somewhere
    config.MonthsRow <-config.MonthsRow-2
    config.DataStartRow<- config.DataStartRow-2
    
    //let b=new a()
  //  let a= CsvFile.Load("")
    let csvs= 
        Directory.EnumerateFiles("./DataSource/")
        |>Seq.map CsvFile.Load
        |>Seq.toList
    let data=csvs|>List.map(fun csv->ConvertFromCsv(csv))
    let processsed=
        data
        |>List.map ((Seq.toList>>List.map (Seq.toList))>>processFile config)
        |>(combineData2 config.MonthsPerFile)
        |>createLists config.MonthsPerFile//here is where we should combine the data from multiple files and pass it into createLists
  //  printfn "%A" processsed
    
    try Directory.CreateDirectory("./output/")|>ignore with|_->()
    
    processsed
    |>List.map (prepData>>makecsv)
    |>List.iteri(fun i x->  File.WriteAllText("./output/"+i.ToString()+".csv",x) )   
  (*   let res=b.Data|>Seq.skip (config.MonthsRow-1)
    let dataIndexes= getRelevantColumns b.Data config.MonthsRow
    let res2= 
        takedata b.Data (config.DataStartRow-1) dataIndexes
        |>removeEmptyRows
        |>seperateNames
        //here is where we should combine the data from multiple files and pass it into createLists
        |>createLists 1
       
    printfn "%A" res2 *)
    0 // return an integer exit code