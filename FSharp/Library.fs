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

    let shellSortByPrice (arr:Product[]) =
        let mutable gap = arr.Length / 2
        while gap > 0 do
            for i in gap..arr.Length-1 do
                let mutable temp = arr.[i]
                let mutable j = i
                while j >= gap && arr.[j-gap].Price > temp.Price do
                    arr.[j] <- arr.[j-gap]
                    j <- j - gap
                arr.[j] <- temp
            gap <- gap / 2

    let shellSortByDiscount (arr:Product[]) =
        let mutable gap = arr.Length / 2
        while gap > 0 do
            for i in gap..arr.Length-1 do
                let mutable temp = arr.[i]
                let mutable j = i
                while j >= gap && arr.[j-gap].Discount > temp.Discount do
                    arr.[j] <- arr.[j-gap]
                    j <- j - gap
                arr.[j] <- temp
            gap <- gap / 2

    let getHtml(url: string) = async {
        use client: HttpClient = new HttpClient()
        let! (response: string) = client.GetStringAsync(url) |> Async.AwaitTask
        return response
    }

    let compoundUrl(url:string, pageNumber: int, input: string) =
        let words = input.Split ' ' 

        let mutable urlOutput = url + "proizvodi/page-" + string(pageNumber) + "/?search="
        for word in words do
            urlOutput <- urlOutput + word + "+"

        urlOutput.Remove(urlOutput.Length - 1)

    let getProductFromHtml(html: HtmlNode) =
           let titleTag = html.Descendants().FirstOrDefault(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "title"))
           let link = titleTag.Descendants().FirstOrDefault(fun node -> node.Name = "a")
           let name: string = link.GetAttributeValue("title", "")
           let url: string = link.GetAttributeValue("href", "")

           let pricesWrapperTag = html.Descendants().FirstOrDefault(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "prices-wrapper"))
           let currentPriceTag = pricesWrapperTag.Descendants().FirstOrDefault(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && (attr.Value = "current-price price-with-discount" || attr.Value = "current-price ")))
           let price = float (currentPriceTag.InnerText.Trim().Substring(0, 5).Replace(",", "."))

           let mutable discount: int = 0

           try
               let discountDiv = pricesWrapperTag.Descendants().Single(fun node -> node.Name = "div" && node.Attributes.Any(fun attr -> attr.Name = "class" && attr.Value = "price-discount 2"))
               let discountTrimmed = discountDiv.InnerText.Trim()
               discount <- int (discountTrimmed.Substring(discountTrimmed.Length - 3, 2))
           with
               | :? System.InvalidOperationException as ex ->
               discount <- 0

           Array.create 1 (Product(name, price, discount, url))

    let mainMenu() =
        printfn "%s" "1) Sortiraj po cijeni - od najnize"
        printfn "%s" "2) Sortiraj po cijeni - od najvise"
        printfn "%s" "3) Sortiraj po popustu - od najnizeg"
        printfn "%s" "4) Sortiraj po popustu - od najviseg"
        printfn "%s" "0) Kraj koristenja"
        printfn "%s" "\n"

        printf "%s" "Odabir: " 
        Console.ReadLine()

    let printProducts(products: Product[], n: int) =
        for i in 0..n-1 do
            printfn "%s" ("Ime: " + products.[i].Name)
            printfn "%s" ("Cijena: " + string(products.[i].Price) + "e")
            printfn "%s" ("Popust: " + string(products.[i].Discount) + "%")
            printfn "%s" ("URL: " + products.[i].Url)
            printfn "%s" "\n"

    let main() =
        let input = Console.ReadLine()

        let mutable url: string = "https://www.sportvision.hr/"
        url <- compoundUrl(url, 0, input)

        let mutable html: string = getHtml(url) |> Async.RunSynchronously
        

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

        printfn "%s" ("Found total of " + string(numberOfPages) + " pages\n")
        printfn "%s" ("Going through page 1")

        let mutable productTags = doc.DocumentNode.SelectNodes("//div[contains(@class,'item') and contains(@class, 'product-item')]")

        for tag in productTags do
            products <- Array.append products (getProductFromHtml(tag))

        if numberOfPages > 1 then
            for i in 1..numberOfPages-1 do
                printfn "%s" ("Going through page " + string(i+1))
                url <- compoundUrl("https://www.sportvision.hr/", i, input)
                html <- getHtml(url) |> Async.RunSynchronously
                (doc.LoadHtml(html))
                let newProducts = doc.DocumentNode.SelectNodes("//div[contains(@class,'item') and contains(@class, 'product-item')]")
                
                for tag in newProducts do
                    products <- Array.append products (getProductFromHtml(tag))

        let mutable menuChoice = mainMenu()
        printfn "%s" "\n"

        while menuChoice <> "0" do

            printf "%s" "Koliko proizvoda za prikazati: "
            let n = int(Console.ReadLine())


            if menuChoice = "1" then
                shellSortByPrice products
                printProducts(products, n)
            else if menuChoice = "2" then
                shellSortByPrice products
                printProducts((Array.rev products), n)
            else if menuChoice = "3" then
                shellSortByDiscount products
                printProducts(products, n)
            else if menuChoice = "4" then
                shellSortByDiscount products
                printProducts((Array.rev products), n)
            else
                printfn "%s" "Krivi unos!"

            menuChoice <- mainMenu()
            printfn "%s" "\n"
        
        1

    main() |> ignore