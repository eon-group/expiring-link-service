# expiring-link-service

Pass in a url and some config, get back a different url that expires in X amount of time after it was created. If url is NOT expired, it will redirect to the configured url, otherwise it will redirect to a link expired page.


POST /create

```
{
    // URL to redirect to if the link is valid
    "url": "http://www.foo.com?bar=baz",
    // Minutes from creation time until link expires
    "expiresIn": 30,
    // If the link should expire immediately after being accessed
    "expiresOnAccess": true,
    // Custom Url to redirect to, if the link is expired
    "expiredRedirectUrl": "http://www.my-site.com/expired"
  }
```

```
response 
{
   "url": "{function_app_host}/r/{guid}"
}
```


GET /r/{guid}
publicly available interface


![expiring-link](https://user-images.githubusercontent.com/6306390/225763499-ce6b0cdb-58c8-4a21-a497-0b47faef9be2.png)
