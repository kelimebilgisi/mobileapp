using System;
using NUnit.Framework;
using Xamarin.UITest;
using Toggl.Tests.UI.Extensions;
using System.Threading.Tasks;
using Toggl.Tests.UI.Helpers;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class SignUpTests
    {
        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForSignUpScreen();
        }

        [Test]
        public void SigningUpWithCorrectCredentialsLeadsToTheMainScreen()
        {
            var email = randomEmail();
            var password = "qwerty123";

            app.Tap(SignUp.EmailText);
            app.EnterText(email);
            app.Tap(SignUp.PasswordText);
            app.EnterText(password);

            app.WaitForDefaultCountryToBeAutoSelected();
            app.Tap(SignUp.SignUpButton);
            app.Tap(SignUp.GdprButton);

            app.WaitForElement(Main.StartTimeEntryButton);
        }

        [Test]
        public async Task SigningUpWithExistingAccountShowsTheErrorLabel()
        {
            var email = randomEmail();
            var password = await User.Create(email);

            app.Tap(SignUp.EmailText);
            app.EnterText(email);
            app.Tap(SignUp.PasswordText);
            app.EnterText(password);

            app.WaitForDefaultCountryToBeAutoSelected();
            app.Tap(SignUp.SignUpButton);
            app.Tap(SignUp.GdprButton);

            app.WaitForElement(SignUp.ErrorLabel);
        }

        [Test]
        public void RejectingTermsAndConditionsCancelsSignUp()
        {
            var email = randomEmail();
            var password = "asdads";

            app.Tap(SignUp.EmailText);
            app.EnterText(email);
            app.Tap(SignUp.PasswordText);
            app.EnterText(password);

            app.WaitForDefaultCountryToBeAutoSelected();
            app.Tap(SignUp.SignUpButton);
            app.RejectTerms();

            app.WaitForNoElement(Main.StartTimeEntryButton);
        }

        [Test]
        public void TheSelectCountrySearchFiltersCountriesByName()
        {
            var countryNameToSearch = "Japan";

            app.WaitForDefaultCountryToBeAutoSelected();
            app.Tap(SignUp.PickCountry);
            app.WaitForElement(SelectCountry.SearchCountryField);

            app.WaitForNoElement(countryNameToSearch);

            app.Tap(SelectCountry.SearchCountryField);
            app.EnterText(countryNameToSearch);
            app.WaitForElementWithText(SelectCountry.CountryNameLabel, countryNameToSearch);
        }

        [Test]
        public void TheSelectCountryBackButtonGoesBackToTheSignUpScreen()
        {
            app.WaitForDefaultCountryToBeAutoSelected();

            app.Tap(SignUp.PickCountry);
            app.WaitForElement(SelectCountry.SearchCountryField);
            app.DismissKeyboard();
            app.NavigateBack();
            app.WaitForElement(SignUp.PickCountry);
        }


        [Test]
        public void SelectingACountryFromTheCountryListDisplaysItsNameInTheSignUpScreen()
        {
            var countryNameToSearch = "Japan";
            app.WaitForDefaultCountryToBeAutoSelected();
            app.Tap(SignUp.PickCountry);
            app.WaitForElement(SelectCountry.SearchCountryField);

            app.WaitForNoElement(countryNameToSearch);
            app.Tap(SelectCountry.SearchCountryField);
            app.EnterText(countryNameToSearch);
            app.WaitForElementWithText(SelectCountry.CountryNameLabel, countryNameToSearch);
            app.Tap(x => x.Marked(SelectCountry.CountryNameLabel).Text(countryNameToSearch));

            app.WaitForElementWithText(SignUp.PickCountry, countryNameToSearch);
        }

        [Test]
        public void TheSelectCountryCanBeOpenedAgainAfterItIsClosed()
        {
            app.WaitForDefaultCountryToBeAutoSelected();
            app.Tap(SignUp.PickCountry);
            app.WaitForElement(SelectCountry.SearchCountryField);

            app.NavigateBack();
            app.WaitForElement(SignUp.PickCountry);
            app.Tap(SignUp.PickCountry);

            app.WaitForElement(SelectCountry.SearchCountryField);
        }

        private string randomEmail()
            => $"{Guid.NewGuid().ToString()}@toggl.space";
    }
}
