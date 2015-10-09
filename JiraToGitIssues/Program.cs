using Nito.AsyncEx;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JiraToGitIssues
{
    class Program
    {
        static void Main(String[] args)
        {
            AsyncContext.Run(() => MainAsync(args));
        }

        static async void MainAsync(string[] args)
        {
            const String File = "FILENAME";
            const String repoOwner = "REPO OWNER";
            const String repo = "REPOSITORY";
            const string GitToken = "TOKEN";
            Dictionary<string,string> UserMapping = new Dictionary<string, string> {
                {"JohnDorian", "jDorian"},
                {"BobKelso", "bKelso"},
                {"ElliotReid","eReid"}                
            }; 
            XDocument xdoc = XDocument.Load(File);
            var Git = new GitHubClient(new ProductHeaderValue("JiraImporter"));
            var TokenAuth = new Credentials(GitToken);
            Git.Credentials = TokenAuth;
             
            var items = from p in xdoc.Descendants("item") select p;
            foreach(var i in items)
            {
                Console.WriteLine("Item: " + i.Descendants("summary").First().Value);
                var Issue = new NewIssue(i.Descendants("summary").First().Value);
                Issue.Body = i.Descendants("description").First().Value;                
                Issue.Assignee = UserMapping[i.Descendants("assignee").First().Attribute("username").Value];
                var itask = Git.Issue.Create(repoOwner, repo, Issue);
                var issue = await itask;

                if (i.Descendants("resolution").First().Value == "Done")
                {
                    var issueUpdate = issue.ToUpdate();
                    issueUpdate.State = ItemState.Closed;
                    await Git.Issue.Update(repoOwner, repo, issue.Number, issueUpdate);
                }
                if(i.Descendants("comments").Count()>0)
                    foreach(var comment in i.Descendants("comments").First().Descendants("comment"))
                    {
                        var ctask = Git.Issue.Comment.Create(repoOwner, repo, issue.Number, UserMapping[comment.Attribute("author").Value] + " commented: " + comment.Value);                    
                        var createdComment = await ctask;
                    }                
           }
        }
    }
}
