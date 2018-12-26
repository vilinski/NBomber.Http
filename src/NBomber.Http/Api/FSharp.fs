﻿module NBomber.Http.FSharp.HttpStep

open System
open System.Net.Http

open FSharp.Control.Tasks.V2.ContextInsensitive

open NBomber.Contracts
open NBomber.FSharp
open NBomber.Http

let private createMsg (req: HttpRequest) =
    let msg = new HttpRequestMessage()
    msg.Method <- HttpMethod(req.Method)
    msg.RequestUri <- Uri(req.Url)
    msg.Version <- Version.Parse(req.Version)
    msg.Content <- req.Body
    req.Headers |> Map.iter(fun name value -> msg.Headers.TryAddWithoutValidation(name, value) |> ignore)
    msg

let createRequest (method: string, url: string) =
    { Url = url; Version = "1.1"; Method = method
      Headers = Map.empty; Body = Unchecked.defaultof<HttpContent> }

let withHeader (name: string, value: string) (req: HttpRequest) =
    { req with Headers = req.Headers.Add(name, value) }  

let withVersion (version: string) (req: HttpRequest) =
    { req with Version = version }     

let withBody (body: HttpContent) (req: HttpRequest) =
    { req with Body = body }    

let private pool = ConnectionPool.create("nbomber.http.pool", (fun () -> new HttpClient()), connectionsCount = 1)

let build (name: string) (req: HttpRequest) =
    Step.createPull(name, pool, fun context -> task { 
        let msg = createMsg(req)
        let! response = context.Connection.SendAsync(msg)
        
        let responseSize =
            if response.Content.Headers.ContentLength.HasValue then 
               response.Content.Headers.ContentLength.Value |> Convert.ToInt32
            else
               0

        match response.IsSuccessStatusCode with
        | true  -> return Response.Ok(sizeBytes = responseSize) 
        | false -> return Response.Fail()
    })