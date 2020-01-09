nuget restore
msbuild QnABot.sln -p:DeployOnBuild=true -p:PublishProfile=csf-web-qna-bot-bot-Web-Deploy.pubxml -p:Password=MG095ELhuGluXjRlnF84wHxpWu8F7vtJtk2C74X0AoLerksGveg2AtGmQAeD

