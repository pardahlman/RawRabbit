# Request/Reply
The standard way of performing RPC calls is to setup a private queue that the responder publish the response on. However, this is not the most efficient way of doing this;

> [...] The client can declare a single-use queue for each request-response pair. But this is inefficient; even a transient unmirrored queue can be expensive to create and then delete (compared with the cost of sending a message). This is especially true in a cluster as all cluster nodes need to agree that the queue has been created, even if it is unmirrored. 
>
> -- https://www.rabbitmq.com/direct-reply-to.html

The default implementation of `RawRabbit` uses the direct RPC calls, which has the effect that `RawRabbit` performs up to 29% faster than other dot.net clients.