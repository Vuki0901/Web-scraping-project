using System;
using System.Net.Http;
using System.Linq;
using HtmlAgilityPack;
using System.Diagnostics;
using System.Xml.Linq;
using System.Drawing;
using System.Collections.Generic;

static class Program
{

    static void Main(string[] args)
    {
        var input = Console.ReadLine()!;

        string url = "https://www.sportvision.hr/";

        url = CompoundUrl(url, 0, input);

        string html = GetHtml(url).Result;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        int numberOfPages = 1;
        IList<Product> products = new List<Product>();

        try
        {
            var paginationTag = doc.DocumentNode.SelectNodes("//ul[contains(@class, 'pagination')]")[0];
            var numberTag = paginationTag.Descendants().Single(node => node.Name == "li" && node.Attributes.Any(attr => attr.Name == "class" && attr.Value == "number"));
            int.TryParse(numberTag.InnerText, out numberOfPages);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("Found total of " + numberOfPages.ToString() + " pages\n");
        Console.WriteLine("Going through page 1");

        var productTags = doc.DocumentNode.SelectNodes("//div[contains(@class,'item') and contains(@class, 'product-item')]");

        foreach(var tag in productTags)
        {
            products.Add(GetProductFromHtml(tag));
        }

        if(numberOfPages > 1)
        {
            for(var i = 1; i < numberOfPages; i++)
            {
                Console.WriteLine("Going through page " + (i + 1).ToString());
                url = CompoundUrl("https://www.sportvision.hr/", i, input);
                html = GetHtml(url).Result;
                doc.LoadHtml(html);
                var newProducts = doc.DocumentNode.SelectNodes("//div[contains(@class,'item') and contains(@class, 'product-item')]");

                foreach(var tag in newProducts)
                {
                    products.Add(GetProductFromHtml(tag));
                }
            }
        }

        Console.WriteLine();
        var menuChoice = MainMenu();

        while(menuChoice != "0")
        {
            Console.Write("Koliko proizvoda za prikazati: ");
            _ = int.TryParse(Console.ReadLine(), out int n);

            if(menuChoice == "1")
            {
                ShellSortByPrice(products);
                PrintProducts(products, n);
            } else if(menuChoice == "2")
            {
                ShellSortByPrice(products);
                PrintProducts(products.Reverse().ToList(), n);
            } else if(menuChoice == "3")
            {
                ShellSortByDiscount(products);
                PrintProducts(products, n);
            } else if(menuChoice == "4")
            {
                ShellSortByDiscount(products);
                PrintProducts(products.Reverse().ToList(), n);
            } else
            {
                Console.WriteLine("Krivi unos!");
            }

            menuChoice = MainMenu();
            Console.WriteLine();
        }
    }

    class Product
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public int? Discount { get; set; }
        public string? Url { get; set; }

        public Product(string? name, decimal price, int discount, string? url)
        {
            Name = name;
            Price = price;
            Discount = discount;
            Url = url;
        }
    }

    private static void ShellSortByPrice(IList<Product> products)
    {
        int n = products.Count;
        int gap = n / 2;

        while (gap > 0)
        {
            for (int i = gap; i < n; i++)
            {
                Product temp = products[i];
                int j = i;

                while (j >= gap && products[j - gap].Price > temp.Price)
                {
                    products[j] = products[j - gap];
                    j -= gap;
                }

                products[j] = temp;
            }

            gap /= 2;
        }
    }

    private static void ShellSortByDiscount(IList<Product> products)
    {
        int n = products.Count;
        int gap = n / 2;

        while (gap > 0)
        {
            for (int i = gap; i < n; i++)
            {
                Product temp = products[i];
                int j = i;

                while (j >= gap && products[j - gap].Discount > temp.Discount)
                {
                    products[j] = products[j - gap];
                    j -= gap;
                }

                products[j] = temp;
            }

            gap /= 2;
        }
    }

    private static async Task<string> GetHtml(string url)
    {
        using var client = new HttpClient();
        return await client.GetStringAsync(url);
    }

    private static string CompoundUrl(string url, int pageNumber, string input)
    {
        var words = input.Split(" ");

        var urlOutput = url + "proizvodi/page-" + pageNumber.ToString() + "/?search=";
        foreach(var word in words)
        {
            urlOutput += word + "+";
        }

        return urlOutput.Remove(urlOutput.Length - 1);
    }

    private static Product GetProductFromHtml(HtmlNode html)
    {
        var titleTag = html.Descendants().FirstOrDefault(node => node.Name == "div" && node.Attributes.Any(attr => attr.Name == "class" && attr.Value == "title"));
        var link = titleTag?.Descendants().FirstOrDefault(node => node.Name == "a");
        var name = link?.GetAttributeValue("title", "");
        var url = link?.GetAttributeValue("href", "");

        var pricesWrapperTag = html.Descendants().FirstOrDefault(node => node.Name == "div" && node.Attributes.Any(attr => attr.Name == "class" && attr.Value == "prices-wrapper"));
        var currentPriceTag = pricesWrapperTag?.Descendants().FirstOrDefault(node => node.Name == "div" && node.Attributes.Any(attr => attr.Name == "class" && (attr.Value == "current-price price-with-discount" || attr.Value == "current-price ")));
        var trimmedCurrentPriceTag = currentPriceTag?.InnerText.Trim();
        if (!decimal.TryParse(trimmedCurrentPriceTag![..(trimmedCurrentPriceTag!.Length - 1)], out decimal price))
        {
            Console.WriteLine("Failed to get price for " + name);
            price = 0;
        }

        var discount = 0;

        try
        {
            var discountDiv = pricesWrapperTag?.Descendants().Single(node => node.Name == "div" && node.Attributes.Any(attr => attr.Name == "class" && attr.Value == "price-discount 2"));
            var discountTrimmed = discountDiv?.InnerText.Trim();
            _ = int.TryParse(discountTrimmed?.Substring(discountTrimmed.Length - 3, 2), out discount);
        }
        catch (Exception)
        {
            discount = 0;
        }

        return new Product(name, price, discount, url);
    }

    private static string MainMenu()
    {
        Console.WriteLine("1) Sortiraj po cijeni - od najnize");
        Console.WriteLine("2) Sortiraj po cijeni - od najvise");
        Console.WriteLine("3) Sortiraj po popustu - od najnizeg");
        Console.WriteLine("4) Sortiraj po popustu - od najviseg");
        Console.WriteLine("0) Kraj koristenja");
        Console.WriteLine("\n");

        Console.Write("Odabir: ");
        return Console.ReadLine()!;
    }

    private static void PrintProducts(IList<Product> products, int n)
    {
        if(n > products.Count)
        {
            n = products.Count;
        }
        for(var i = 0; i < n; i++)
        {
            Console.WriteLine("Ime: " + products[i].Name);
            Console.WriteLine("Cijena: " + products[i].Price.ToString() + "e");
            Console.WriteLine("Popust: " + products[i].Discount + "%");
            Console.WriteLine("URL: " + products[i].Url);
            Console.WriteLine();
        }
    }
}