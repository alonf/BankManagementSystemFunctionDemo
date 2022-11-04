# Microservice Demo - Bank Management System
This repository contains the Azure Functions App demo application of my lecture: **"Microservice Solution With Serverless Functions and Serverless Containers – Pros and Cons."**

There is another repo for demonstrating the Microservices-based solution with Dapr and ACA.

The demo facilitates a naive bank account distributed system. These are the main user stories:
1. As a teller, I'd like to register new bank customers
2. As a teller, I'd like to view customer information
3. As a customer, I'd like to deposit
4. As a customer, I'd like to withdraw
5. As a customer, I'd like to view my account balance
6. As a customer, I'd like to view the personal history of transactions.

## The system contains the following services as function app:
- Account Manager - serves as a facade and orchestrator of the system operations.
- User Information Accessor (User Info) - serves as a CRUD mediator with the database for user-related information.
Insert and update operations are handled asynchronously using a queue. Operation outcomes go to a result queue. Query operations are conducted synchronously.
- Checking Account Accessor - serves as the CRUD mediator with the database for account status and transaction information.
- Liability Validator Engine - checks if it is allowed to withdraw money from the account.
- Notification Manager - signal client applications about the outcome of asynchronous requests

The services use Function triggers and bindings. Locally, they run with the func.exe command. Other resources such as storage blob, queues, Redis, and SignalR emulator run under docker-compose. The CosmosDB emulator runs on Windows to overcome the certificate problem. Finally, the integration tests run on the host. On GitHub, for CI/CD, most of the services and the support services run locally, while SignalR and CosmosDB are hosted on Azure. 
An entire GitHub Actions and Bicep DevOps pipeline builds and tests the code, pushes the build artifacts to Azure, and deploys and configures the Azure Function App. 
