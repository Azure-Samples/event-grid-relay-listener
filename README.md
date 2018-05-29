# Event Grid Relay listener

The sample shows you how to use Azure Relay to listen to Azure Event Grid events directly on your console. Send events from any Event Publisher to your relay endpoint and stream them real time to your console for monitoring or app orchestration.

## Features

This project framework provides the following features:

* Event Grid WebHook endpoint validation
* Relay listener
* Framework for custom event handling

## Getting Started

### Prerequisites

- The sample requires an [Azure Relay Service WCF-Relay endpoint](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-wcf-dotnet-get-started)

### Quickstart

1. [Create an Azure Relay namespace](https://docs.microsoft.com/en-us/azure/service-bus-relay/relay-create-namespace-portal).
2. Create a WCF-Relay in the new Azure Relay namespace with the following values:

    Setting | Value
    ------------ | -------------
    Name | gridservicelistener
    Relay Type | Http
    Requires Client Authorization | Unchecked (False)

3. Set the following values in GridRelayListener Program.cs:
    * `"<yourServiceBusNamespace>"` with the Relay namespace you just created.
    * `"RootManageSharedAccessKey"` with the Relay namespace's shared access policy name if you changed this from the default. *You do not need to do this if you left the name as default.*
    * `"<yourServiceBusKeyValue>"` with the shared access policy's primary or secondary key value.
4. Create an [Event Grid Subscription](https://docs.microsoft.com/en-us/azure/event-grid/overview):
    * Set `https://<yourServiceBusNamespace>/gridservicelistener?code=<yourServiceBusKeyValue>` as the subscriber endpoint.
5. Run the GridRelayListener sample and send events to your Event Grid subscription using your preferred [event publisher](https://docs.microsoft.com/en-us/azure/event-grid/overview#built-in-publisher-and-handler-integration).

## Demo

Use the [Blob Storage eventing](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-event-quickstart?toc=%2fazure%2fevent-grid%2ftoc.json) quick start to get set up sending events from your storage account and use `https://<yourServiceBusNamespace>/gridservicelistener?code=<yourServiceBusKeyValue>` as your subscriber endpoint. Add and delete files from your Blob Storage account to see events appear in your running console.

## Resources

* Learn more about [Azure Event Grid](https://docs.microsoft.com/en-us/azure/event-grid/overview)
