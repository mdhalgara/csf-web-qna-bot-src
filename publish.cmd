nuget restore
msbuild QnABot.sln -p:DeployOnBuild=true -p:PublishProfile=csf-web-qna-bot-Web-Deploy.pubxml -p:Password=EEwmJ58mbbgK8doyTrguMm4QG9icgX1jhX5vCCiBfkvaBJakTi9rwYEmhsS2

