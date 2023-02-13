namespace FSharp

open System
open System.Net.Http
open System.Linq
open HtmlAgilityPack

module Scraper =
    type Product(name: string, price: float, discount: int, url: string) =
        let mutable _name = name
        let mutable _price = price
        let mutable _discount = discount
        let mutable _url = url

        member this.Name with get() = _name and set(value) = _name <- value
        member this.Price with get() = _price and set(value) = _price <- value
        member this.Discount with get() = _discount and set(value) = _discount <- value
        member this.Url with get() = _url and set(value) = _url <- value

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

    let getProductFromHtml(html: HtmlNode) =
           let titleTag = html.Descendants().Single(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "title"))
           let link = titleTag.Descendants().Single(fun node -> node.Name = "a")
           let name: string = link.GetAttributeValue("title", "")
           let url: string = link.GetAttributeValue("href", "")

           let pricesWrapperTag = html.Descendants().Single(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "prices-wrapper"))
           let currentPriceTag = pricesWrapperTag.Descendants().Single(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "current-price price-with-discount"))
           let price = float (currentPriceTag.InnerText.Trim().Substring(0, 5).Replace(",", "."))

           let discountDiv = pricesWrapperTag.Descendants().Single(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "price-discount 2"))
           let discountTrimmed = discountDiv.InnerText.Trim()
           let discount = int (discountTrimmed.Substring(discountTrimmed.Length - 3, 2))

           Array.create 1 (Product(name, price, discount, url))

    let main() =
        let mutable url: string = "https://www.sportvision.hr/"
        url <- compoundUrl(url)

        let html: string = getHtml(url) |> Async.RunSynchronously

        let doc = HtmlDocument()
        doc.LoadHtml(html)   

        let mutable numberOfPages: int = 1
        let mutable products = [||]

        try
            let paginationTag = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'pagination')]")[0]
            let numberTag = paginationTag.Descendants().Single(fun node -> node.Name = "li" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "number"))
            numberOfPages <- int numberTag.InnerText
        with
            | :? System.InvalidOperationException as ex ->
            printfn "Error: %s" ex.Message

        //li sa class = number - koliko ima takvih tagova toliko stranica
        //for i in numberTags.length - 1 => getHtml -> url = /proizvodi/page-1 za drugu stranicu
        //prva stranica dolazi normalno iz /proizvodi/?search=patike, a za 2. nadalje se navodi page
        

        let productTags = doc.DocumentNode.SelectNodes("//div[contains(@class,'item') and contains(@class, 'product-item')]")
        //Svi div class='item product-item' sa 1. stranice!!

        for tag in productTags do
            products <- Array.append products (getProductFromHtml(tag))
        
        for product in products do
            printfn "%s" product.Name
            printfn "%f" product.Price
            printfn "%d" product.Discount
            printfn "%s" product.Url

        1

    main() |> ignore

