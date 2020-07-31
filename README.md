# dotnet-dind-debugger
Two methods for debugging .NET core containers running in Docker-in-Docker environment using Visual Studio or Visual Studio Code.   

![alt text](https://blog.nestybox.com/assets/dind-privileged.png)
## simple-setup

This is more standard approach for attaching debugger to a .NET container as it uses configuration as is. However, the configuration is made to work with nested virtualization by executing first to the outer Dockerl layer and then the inner, where the debugger is started.  

Currently the <enter_outer_container_name_here> and <enter_inner_container_name_here> need to be specified manually. Provided debugger-path-script.sh can be used to automatically replace the path. The configuration template included in the simple-setup folder is also shown below: 

```
{
  "version": "0.2.0",
  "adapter": "powershell",
  "adapterArgs": "<enter_project_path_here> ; docker-compose exec -T <enter_container_name_1_here> docker-compose exec -T <enter_container_name_2_here> /vsdbg/vsdbg --interpreter=vscode",
  "languageMappings": {
    "C#": {
      "languageId": "3F5162F8-07C6-11D3-9053-00C04FA302A1",
      "extensions": [ "*" ]
    }
  },
  "exceptionCategoryMappings": {
    "CLR": "449EC4CC-30D2-4032-9256-EE18EB41B62B",
    "MDA": "6ECE07A9-0EDE-45C4-8296-818D8FC401D4"
  },
  "configurations": [
    {
      "name": ".NET Core Docker Attach",
      "type": "coreclr",
      "request": "attach",
      "processName": "dotnet"
    }
  ]
}
```

## 
