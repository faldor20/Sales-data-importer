
module Manipulations
open System
open FSharp.Interop.Excel
open FSharp.Interop.Excel.ExcelProvider
open System.Collections.Generic
open System.IO
open FSharp.Data
type a=ExcelFile<"./SalesAnalysis.xls",HasHeaders=false>

//gets the block of data as defined by the dataStart(row) and dataindexes(columns)  
let takedata (range:IEnumerable<a.Row>) dataStart dataIndexes =
    let dataRange=range|>Seq.skip (dataStart)
    //gets the data as selected by dataIndexes 
    //NOTE: Some data may come back as null.
    let data=
        dataRange
        |>Seq.map(fun x->
            dataIndexes
            |>List.map(fun i->x.TryGetValue i "" )
            |>List.map(fun x->match x with |null->None|y->Some y))
        |>Seq.toList
    data
//selects on the columns containg data, as defined by dataIndexs
let selectColumns  dataIndexs range=
    let rec thing (range:list<int*string option>) (dataIndexs:list<int>) output= 
        match range,dataIndexs with
        |_,[]-> output|>List.rev
        |head::tail,desiredIndex::rest->
            let index,cell= head 
            if index=desiredIndex then
                thing tail rest (cell::output)
            else thing tail dataIndexs output
    
    range|>List.map(fun row->
        let a= row|>List.indexed
        //fast way:
        //thing a dataIndexs []
        //slow way:
        a|>List.choose(fun (i,x)-> if(dataIndexs|>List.contains i)then Some x else None)

    )

let ConvertFromCsv (csv:CsvFile)=
    csv.Rows|>Seq.map(fun r->r.Columns|>Seq.map(fun x->if x="" then None else Some(x)))
//Used to remove the total column at the end
let removeLastColumn (range:list<list<string option>>)=
    range|>List.map(fun x-> x|>List.take (x.Length-1))
///Returns a list of the columns that containin data
let getRelevantColumns (range:list<list<string option>>) monthRow=
    let monthRow= 
        range.[monthRow] //we ake one off for 0 indexing
        
    printfn "Monthrow: %A" monthRow
    let dataIndexes=monthRow|> List.indexed|>List.filter (fun (index,y)->y.IsSome)|>List.map(fun(index,_)->index)
    let a=dataIndexes //there is some chance i need to add a 0 to the beginning if the first column isn't being added
    a
///Removes rows that have nothing in their first row(This should containa heading if there is data)
let removeEmptyRows( list:'a option list list )=
    list|>List.filter(fun x->x.Head.IsSome)
///Takes the first item in the list and moves it to a tuple. In our case this is the customer name
let seperateNames ( list:'a option list list )=
    list|>List.map( fun x->(x.Head.Value,x.Tail))
///returns a list of customers who were paid this month in all the years provided.
///eg customers paid nov 2019 nov 2018 and nov 2017
let getMonthsPayments (paymentsPerMonth:(string*string option list list) list) month=
    paymentsPerMonth|>List.choose(fun (name,payments)->
        if payments.[month]|>List.exists(fun x->x.IsSome) then
            Some (name,payments.[month])
        else None
        )
///Get the current month and previous months payments. 
///Currently highly inneficient becuase it runs get month payments on each month previous to the current one.
let getThisAndPreviousMonthsPayments (paymentsPerMonth:(string*string option list list) list) month=
    //get a list of all clients needed in that month by getting the names of the cleints need by all previous months plus this one

    let neededClients= 
        [0..month]
        |>List.collect(fun thisMonth->
            getMonthsPayments paymentsPerMonth thisMonth
            |>List.map(fun (x,y)->x)
        )
        |>List.distinct
    //filter this month by clinets in prvious months
    let res=
        paymentsPerMonth
        |>List.filter(fun (x,y)-> (neededClients|>List.contains x))
        |>List.map(fun(x,y)->x,y.[month])
    res

///Appends the name to the beginning of the list becuase as a csv or spreadsheet the name is the first column
let prepData (list: list<'a * list<option<'a>>>)=
    list|>List.map(fun (x,y)->(Some x)::y)

let csvPair (state:string) (y:string option )=
    let csvValue=
        if y.IsSome then
        //Some values have "," in them which would stuff up our csv
            y.Value.Replace(",","")
        else " "
    sprintf "%s%s,"state csvValue
///Turns the data into a csv
let makecsv (data:string option list list) =
    data|>List.fold(fun state x->
        (sprintf "%s%s \n" 
            state 
            (x|>List.fold (csvPair) "")
        )
    ) ""
 

//Why we transpose:
//before:  [ year1[ month1y1,month2y1,month3y1], year2[m1y2,m1y2,m1y2]]
//after: [month1[m1y1,m1y2],month2[m2y1,m2y2]]etc etc
let createLists monthsPerYear ( list: (string*string option list) list ) =
    let paymentsPerMonth=
        list
        |>List.map(fun (x,y)->
            x,
            y
            //seperate payments by year
            |>List.chunkBySize monthsPerYear
            //turn the list of payhments in each year, into paymenst per month
            |>List.transpose)
    //TODO: filter the incoming list to only contain a set selection of years.
    [0..monthsPerYear-1]|>List.map(fun month->

        let payments= getThisAndPreviousMonthsPayments paymentsPerMonth month
        payments
     )