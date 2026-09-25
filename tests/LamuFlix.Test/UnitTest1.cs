using System;
using System.Collections.Generic;
using System.Diagnostics;
using LamuFlix.Data.Models;
using LamuFlix.Tests.Common;
using LamuFlix.Web.Services;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Xunit;

namespace LamuFlix.Test
{
    public class UnitTest1
    {
        [Fact]
        public void AssistirFilme_WhenLocalPlayDisabled_ThrowsInvalidOperationException()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "false"
                })
                .Build();

            using (var context = LamuFlixContextFactory.CreateContext())
            {
                var service = new MovieService(context, config);

                var ex = Should.Throw<InvalidOperationException>(() => service.PlayMovie(1));
                ex.Message.ShouldBe("LocalPlay is disabled.");
            }
        }

        [Fact]
        public void AssistirFilme_WhenLocalPlayEnabledAndPlayerConfigured_StartsProcessWithConfiguredPlayerAndArgumentList()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "true",
                    ["Features:PlayerPath"] = @"C:\Players\vlc.exe"
                })
                .Build();

            using (var context = LamuFlixContextFactory.CreateContext())
            {
                var movie = new Movie
                {
                    Id = 1,
                    Title = "Test Movie",
                    Location = @"C:\TestLibrary\Test[2020]\test.mkv",
                    Format = ".mkv"
                };
                context.Movies.Add(movie);
                context.SaveChanges();

                ProcessStartInfo? capturedStartInfo = null;
                var service = new MovieService(context, config)
                {
                    ProcessStarter = psi =>
                    {
                        capturedStartInfo = psi;
                        return null;
                    }
                };

                service.PlayMovie(1);

                capturedStartInfo.ShouldNotBeNull();
                capturedStartInfo.UseShellExecute.ShouldBeTrue();
                capturedStartInfo.FileName.ShouldBe(@"C:\Players\vlc.exe");
                capturedStartInfo.ArgumentList.Count.ShouldBe(1);
                capturedStartInfo.ArgumentList[0].ShouldBe(movie.Location);
            }
        }

        [Fact]
        public void AssistirFilme_WhenLocalPlayEnabledAndPlayerNull_DefaultsToOsAssociation()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Features:LocalPlay"] = "true"
                })
                .Build();

            using (var context = LamuFlixContextFactory.CreateContext())
            {
                var movie = new Movie
                {
                    Id = 2,
                    Title = "Test Movie OS Assoc",
                    Location = @"C:\TestLibrary\Test[2020]\test.mp4",
                    Format = ".unmappedformat"
                };
                context.Movies.Add(movie);
                context.SaveChanges();

                ProcessStartInfo? capturedStartInfo = null;
                var service = new MovieService(context, config)
                {
                    ProcessStarter = psi =>
                    {
                        capturedStartInfo = psi;
                        return null;
                    }
                };

                service.PlayMovie(2);

                capturedStartInfo.ShouldNotBeNull();
                capturedStartInfo.UseShellExecute.ShouldBeTrue();
                capturedStartInfo.FileName.ShouldBe(movie.Location);
                capturedStartInfo.ArgumentList.Count.ShouldBe(0);
            }
        }
    }
}
