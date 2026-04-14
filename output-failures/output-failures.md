System.ClientModel.ClientResultException: HTTP 401 (: unauthorized)

The `models` permission is required to access this endpoint
   at OpenAI.ClientPipelineExtensions.ProcessMessageAsync(ClientPipeline pipeline, PipelineMessage message, RequestOptions options)
   at OpenAI.Chat.ChatClient.CompleteChatAsync(BinaryContent content, RequestOptions options)
   at OpenAI.Chat.ChatClient.CompleteChatAsync(IEnumerable`1 messages, ChatCompletionOptions options, RequestOptions requestOptions)
   at ResumeRating.Api.Services.GitHubModelsAiService.CallAsync(String prompt) in /Users/janani/Work/GitHub/ResumeRating/ResumeRating.Api/Services/GitHubModelsAiService.cs:line 204
   at ResumeRating.Api.Services.GitHubModelsAiService.EvaluateResumeAsync(String resumeText, JobDescription jobDescription) in /Users/janani/Work/GitHub/ResumeRating/ResumeRating.Api/Services/GitHubModelsAiService.cs:line 71
   at ResumeRating.Api.Services.EvaluationService.EvaluateAsync(String resumeId, String jobDescriptionId) in /Users/janani/Work/GitHub/ResumeRating/ResumeRating.Api/Services/EvaluationService.cs:line 94
   at ResumeRating.Api.Controllers.EvaluationController.Evaluate(EvaluateRequest request) in /Users/janani/Work/GitHub/ResumeRating/ResumeRating.Api/Controllers/EvaluationController.cs:line 26
   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.TaskOfIActionResultExecutor.Execute(ActionContext actionContext, IActionResultTypeMapper mapper, ObjectMethodExecutor executor, Object controller, Object[] arguments)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeActionMethodAsync>g__Awaited|12_0(ControllerActionInvoker invoker, ValueTask`1 actionResultValueTask)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeNextActionFilterAsync>g__Awaited|10_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Rethrow(ActionExecutedContextSealed context)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.Next(State& next, Scope& scope, Object& state, Boolean& isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ControllerActionInvoker.<InvokeInnerFilterAsync>g__Awaited|13_0(ControllerActionInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeFilterPipelineAsync>g__Awaited|20_0(ResourceInvoker invoker, Task lastTask, State next, Scope scope, Object state, Boolean isCompleted)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)
   at Microsoft.AspNetCore.Mvc.Infrastructure.ResourceInvoker.<InvokeAsync>g__Awaited|17_0(ResourceInvoker invoker, Task task, IDisposable scope)
   at Microsoft.AspNetCore.Authorization.AuthorizationMiddleware.Invoke(HttpContext context)
   at Microsoft.AspNetCore.Authentication.AuthenticationMiddleware.Invoke(HttpContext context)
   at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)

HEADERS
=======
Accept: application/json, text/plain, */*
Connection: keep-alive
Host: localhost:5073
User-Agent: Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36
Accept-Encoding: gzip, deflate, br, zstd
Accept-Language: en-GB,en-US;q=0.9,en;q=0.8
Content-Type: application/json
Origin: http://localhost:3000
Referer: http://localhost:3000/
Content-Length: 109
sec-ch-ua-platform: "macOS"
sec-ch-ua: "Google Chrome";v="147", "Not.A/Brand";v="8", "Chromium";v="147"
sec-ch-ua-mobile: ?0
Sec-Fetch-Site: same-site
Sec-Fetch-Mode: cors
Sec-Fetch-Dest: empty
clientid: b360-location
clientsecret: GvdGsg3ADV8pSvLpvYmKAL8KTXKLs9uH