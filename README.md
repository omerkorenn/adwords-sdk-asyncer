# AdWords SDK Asyncer

This small console app, allows to automate the process of creating async method in the core class of AdWords API (`AdWordsApi.cs`)


## Setup

>First step, you will need to manually add the async invoker to the `Common/Lib/AdsSoapClient.cs` :  
The `Common/Lib/AdsSoapClient.cs` class is shared between all versions so this is a one time addition.  
```csharp
protected async Task<object[]> InvokeAsync(string methodName, object[] parameters)
{
    var action = new Func<AsyncCallback, object, IAsyncResult>((y, x) => { return base.BeginInvoke(methodName, parameters, y, x); });

    while (true)
    {
        InitForCall(methodName, parameters);

        try
        {
            return
                await Task.Factory.FromAsync(action,
                    base.EndInvoke,
                    null);
        }
        catch (SoapException ex)
        {
            ErrorHandler errorHandler = CreateErrorHandler();

            Exception customException = GetCustomException(ex);
            if (errorHandler.ShouldRetry(customException))
            {
                errorHandler.PrepareForRetry(customException);
            }
            else
                throw customException;
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            CleanupAfterCall(methodName, parameters);
        }
    }
}
```


>Second step is to run the upgrader, to create all async methods (`get`,`mutate`,`query`,`getResult`)  
Simply by the following command:
`> Upgrader.exe ./AdWords/<ApiVersion>/AdWordsApi.cs`

>Third step, is to compile the library and enjoy (`getAsync`,`mutateAsync`,`queryAsync`,`getResultAsync`)  
which will now be added to all Adwords Services.


