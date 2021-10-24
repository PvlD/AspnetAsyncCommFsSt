namespace HildenCo.Core
open System
    type Product  = 
           {
            Id :string
            Slug :string
            Name:string
            Description:string
            Price:Decimal
            Currency:string
            CreatedOn:DateTime
            LastUpdate:DateTime
           } 
 