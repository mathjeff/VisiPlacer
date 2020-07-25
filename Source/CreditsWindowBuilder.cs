using StatLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

// A CreditsWindowBuilder builds a window containing attributions of ways that users have contributed to the application
namespace VisiPlacement
{
    public class CreditsWindowBuilder
    {
        public CreditsWindowBuilder(LayoutStack layoutStack)
        {
            this.layoutStack = layoutStack;
        }

        public CreditsWindowBuilder AddContribution(AppContribution contribution)
        {
            if (this.contributions.Count > 0)
            {
                AppContribution last = this.contributions.Last();
                if (contribution.DateTime.CompareTo(last) < 0)
                {
                    throw new Exception("Illegal contribution ordering: contribution " + contribution + " mustbe specified before " + last);
                }
            }
            this.contributions.Add(contribution);
            return this;
        }

        private LayoutChoice_Set MakeSublayout(AppContribution contribution)
        {
            // get some properties
            AppContributor who = contribution.Contributor;
            string name = who.Name;
            string when = contribution.DateTime.ToString("yyyy-MM-dd");
            string description = contribution.Description;
            double fontSize = 16;

            // build the name button layout
            LayoutChoice_Set nameLayout;
            if (who.Email != null || who.Website != null)
            {
                // If this contributor provided details, then make a button to show those details
                Vertical_GridLayout_Builder detailBuilder = new Vertical_GridLayout_Builder();
                detailBuilder.AddLayout(new TextblockLayout("Contributor Name: " + name, fontSize));
                if (who.Email != null)
                    detailBuilder.AddLayout(new TextblockLayout("Email: " + who.Email, fontSize));
                if (who.Website != null)
                    detailBuilder.AddLayout(new TextblockLayout("Website: " + who.Website, fontSize));
                nameLayout = new HelpButtonLayout(name, detailBuilder.Build(), layoutStack);
            }
            else
            {
                // If this contributor didn't provide details, then just display their name
                nameLayout = new TextblockLayout(name);
            }

            // build the rest of the layout
            Vertical_GridLayout_Builder fullBuilder = new Vertical_GridLayout_Builder();
            fullBuilder.AddLayout(nameLayout);
            fullBuilder.AddLayout(new TextblockLayout("On " + when + ": " + description, fontSize));
            fullBuilder.AddLayout(new TextblockLayout("Thanks!", fontSize));
            return fullBuilder.Build();
        }
        public LayoutChoice_Set Build()
        {
            Vertical_GridLayout_Builder builder = new Vertical_GridLayout_Builder();
            int minIndex = Math.Max(this.contributions.Count - 3, 0);
            for (int i = this.contributions.Count - 1; i >= minIndex; i--)
            {
                builder.AddLayout(this.MakeSublayout(this.contributions[i]));
            }

            return builder.Build();
        }


        private List<AppContribution> contributions = new List<AppContribution>();
        private LayoutStack layoutStack;
    }

    public class CreditsButtonBuilder
    {
        public CreditsButtonBuilder(LayoutStack layoutStack)
        {
            this.layoutStack = layoutStack;
            this.contentBuilder = new CreditsWindowBuilder(layoutStack);
        }
        public CreditsButtonBuilder AddContribution(AppContribution contribution)
        {
            this.contentBuilder.AddContribution(contribution);
            return this;
        }
        public LayoutChoice_Set Build()
        {
            return new HelpButtonLayout("Credits", this.contentBuilder.Build(), this.layoutStack);
        }
        private CreditsWindowBuilder contentBuilder;
        private LayoutStack layoutStack;
    }

    public class AppContribution
    {
        public AppContribution(AppContributor contributor, DateTime when, string description)
        {
            this.Contributor = contributor;
            this.DateTime = when;
            this.Description = description;
        }

        public DateTime DateTime;
        public AppContributor Contributor;
        public string Description;
    }

    public class AppContributor
    {
        public AppContributor(string name, string email = null, string website = null)
        {
            this.Name = name;
            this.Email = email;
            this.Website = website;
        }

        public string Name;
        public string Email;
        public string Website;
    }

}
