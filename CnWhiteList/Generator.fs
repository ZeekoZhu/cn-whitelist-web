module CnWhiteList.Generator

open System
open System.IO
open System.Net.Http
open System.Text
open Microsoft.Extensions.Logging

let cnDomainsUrl =
    "https://raw.githubusercontent.com/felixonmars/dnsmasq-china-list/master/accelerated-domains.china.conf"

let cnCdnDomainsUrl =
    "https://raw.githubusercontent.com/mawenjian/china-cdn-domain-whitelist/master/china-cdn-domain-whitelist.txt"

// ---------------------------
// Functions
// ---------------------------

let generateWhiteList (logger: ILogger) (httpFactory: IHttpClientFactory) =
    let http = httpFactory.CreateClient()
    let step msg fn p =
        try
            fn p
        with ex -> logger.LogError(ex, msg); raise ex

    let parseCnDomain (str: string) = str.Split [| '/' |] |> Array.item 1

    let getString (url : string) =
        http.GetStringAsync url
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let resolveFile path =
        Path.Join (Directory.GetCurrentDirectory(), path)

    let nonEmptyString = String.IsNullOrWhiteSpace >> not

    let downloadCnCdnDomains () =
        getString cnCdnDomainsUrl
        |> (fun x -> x.Split [| '\n' |])
        |> Seq.filter (fun x -> not (x.StartsWith "#"))
        |> Seq.filter (nonEmptyString)

    let downloadCnDomains () =
        getString cnDomainsUrl
        |> (fun x -> x.Split [| '\n' |])
        |> Seq.where (nonEmptyString)
        |> Seq.map parseCnDomain

    let readUserList () =
        File.ReadAllLines <| resolveFile "./user-list.txt"
        |> Seq.filter (nonEmptyString)

    let getAllDomains () =
        Seq.concat
            (seq {
                yield (step "download CN CDN domains" downloadCnCdnDomains) ()
                yield (step "download CN domains" downloadCnDomains) ()
                yield (step "read user list" readUserList) ()
             })
        |> Seq.distinct
        |> Seq.filter (fun domain -> not <| domain.EndsWith ".cn")
        |> Seq.sort
        |> Seq.map (sprintf "||%s")

    let generateRuleList () =
        let sb = StringBuilder()
        sb.Append(File.ReadAllText <| resolveFile "./whitelist.template")
        |> ignore
        getAllDomains ()
        |> Seq.iter (sb.AppendLine >> ignore)
        sb.ToString()

    generateRuleList ()
