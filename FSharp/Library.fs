namespace FSharp

open System
open System.Net.Http
open HtmlAgilityPack
open FSharp.Data
open Microsoft.FSharp.Collections

module Scraper =
    let getHtml(url: string) = async {
        use client: HttpClient = new HttpClient()
        let! (response: string) = client.GetStringAsync(url) |> Async.AwaitTask
        return response
    }

    let compoundUrl(url:string) =
        let input = Console.ReadLine()
        let words = input.Split ' ' 

        let mutable urlOutput = url + "proizvodi?search="
        for word in words do
            urlOutput <- urlOutput + word + "+"

        urlOutput.Remove(urlOutput.Length - 1)

    let main() =
        let mutable url: string = "https://www.sportvision.hr/"
        url <- compoundUrl(url)

        let html: string = getHtml(url) |> Async.RunSynchronously

        let doc = HtmlDocument()
        doc.LoadHtml(html)   

        //li sa class = number - koliko ima takvih tagova toliko stranica
        //for i in numberTags.length - 1 => getHtml -> url = /proizvodi/page-1 za drugu stranicu
        //prva stranica dolazi normalno iz /proizvodi/?search=patike, a za 2. nadalje se navodi page
        
        let productTags = doc.DocumentNode.SelectNodes("//div[contains(@class,'item') and contains(@class, 'product-item')]")
        //Svi div class='item product-item' sa 1. stranice!!

        for tag in productTags do
            printfn "%s" tag.XPath
        
        1

    main() |> ignore
