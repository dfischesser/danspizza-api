# Dan's Pizza API
.NET Core Web API for Dan's Pizza. This API provides the following data:
- Full Menu
- User account creation, authentication, and authorization
- Order fullfillment from authorized users (Employees and higher)
- Azure Maps address search passthrough

- Authentication is provided with bcrypt
- Authorization is provided with Javascript Web Tokens (JWT) using .NET's Authorization middleware

# Endpoints
This service acts as the back-end of Dan's Pizza, enabling the Next.js application to communicate with Dan's Pizza's Azure SQL Database.

## Menu (/api/Menu/Get)
Delivers the current menu items to the user. Data is retrieved and formatted as follows:
- Menu Category (Pizza, Pasta, etc)
- Food Item (Hand-Tossed, Francese, etc)
- Customize Option + Option Item (Size: Large, Meat: Chicken, etc)

## Login (/api/Login)
Verifies email and password combination and returns a JWT
- Accepts:
  - `Email`
  - `Password`
- Returns:
  - `Success` response including JWT stored as a HttpOnly reponse cookie
  
## Create Account (/api/User/Create)
Accepts an email and password. Once the use email is verified as unique, the password is salted and hashed using bcrypt.
A JWT is then generated and encoded, and transmitted as a response cookie.
- Accepts:
  - `Email`
  - `Password`
- Returns:
  - `Success` response including JWT stored as a HttpOnly reponse cookie

## Create Account - Step 2 (/api/User/CreateStep2)
Authorized endpoint to complete account registration
- Accepts:
  - [Cookie] User JWT
  - `First Name`
  - `Last Name`
  - `Phone Number`
  - `Address` (Verified via Azure Maps)
- Returns: 
  - `Success response` including Updated JWT. New JWT includes updated claim for user's First Name

 ## Account (/api/User/Account)
 Retrieves user account information including past orders
 - Accepts:
   - [Cookie] User JWT
   -  `First Name`
   -  `Last Name`
   -  `Phone Number`
   -  `Address`
   -  `Active Orders`
   -  `Past Orders`

## Post Order (/api/Order/Post)
Posts a new order prepared by the user. 
-Accepts: 
  - [Cookie] User JWT (Employee Role)
  - Order JSON. Example: `[{"foodID":2,"menuCategoryID":1,"foodName":"Thin Crust Pizza","price":20.99,"foodOrder":null,"customizeOptions":[{"optionID":1,"optionName":"Size","isMultiSelect":false,"isDefaultOption":true,"price":16.99,"optionItems":[{"foodID":2,"optionID":1,"customizeOption":"Size","customizeOptionOrder":1,"customizeOptionItem":"Medium","customizeOptionItemID":12,"price":16.99,"isMultiSelect":false,"isDefaultOption":true,"customizeOptionItemOrder":null,"createdOn":null,"modifiedOn":null}]}]}]`
- Returns:
  - Success message
 
## Latest Orders (/api/Order/Latest)
Gets a page of active orders (Used in getServerSideProps)
- Accepts: 
  - [Cookie] User JWT (Employee Role)
- Returns:
  - 5 latest orders
 
## Fulfill Order (/api/Order/Fullfill)
Fulfills an order by setting active to 0
- Accepts: 
  - [Cookie] User JWT (Employee Role)
  - Order ID
- Returns:
  - New list of active orders
 
## Order Page (/api/Order/OrderPage)
Gets a page of active orders
- Accepts: 
  - [Cookie] User JWT (Employee Role)
  - Page #
- Returns:
  - 5 orders depending on the page
    
