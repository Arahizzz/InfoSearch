module Dictionary.Parser

open System
open FParsec

type SearchFunction =
    | StartOfWord of string
    | EndOfWord of string
    | MiddleOfWord of string * string
    | WithoutJoker of string
    
let private star = pchar '*'

let private start = many1CharsTill letter star .>>? notFollowedBy letter |>> StartOfWord
let private middle = many1CharsTill letter star .>>.? many1Chars letter .>> notFollowedBy star |>> MiddleOfWord

let private ends = star >>. many1Chars letter .>> notFollowedBy star |>> EndOfWord
let private word = manyChars letter .>>? notFollowedBy star |>> WithoutJoker

let private parser = word <|> middle <|> start <|> ends

let ParseString str = run parser str

let ParseQuery query =
    match (ParseString query) with
    | Success (value, _, _) -> value
    | Failure _ -> raise (ArgumentException("Incorrect query"))

