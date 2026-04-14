using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CityFix.Api.Models;
using Xunit;

namespace CityFix.Api.Tests
{
    public class WorkerRegisterDtoTests
    {
        private static IList<ValidationResult> ValidateModel(WorkerRegisterDto model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model, serviceProvider: null, items: null);

            Validator.TryValidateObject(model, context, results, validateAllProperties: true);

            return results;
        }

        private static WorkerRegisterDto CreateValidModel()
        {
            return new WorkerRegisterDto
            {
                NationalId = "123456789",
                FullName = "Ahmad Akhras",
                Phone = "0501234567",
                Email = "worker@test.com",
                Municipality = "באר שבע",
                Department = "כבישים",
                Password = "123456"
            };
        }

        [Fact]
        public void WorkerRegisterDto_ShouldBeValid_WhenAllFieldsAreCorrect()
        {
            var model = CreateValidModel();

            var results = ValidateModel(model);

            Assert.Empty(results);
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenNationalIdIsNull()
        {
            var model = CreateValidModel();
            model.NationalId = null!;

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.NationalId)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenNationalIdIsEmpty()
        {
            var model = CreateValidModel();
            model.NationalId = "";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.NationalId)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenNationalIdIsWhitespace()
        {
            var model = CreateValidModel();
            model.NationalId = "   ";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.NationalId)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenFullNameIsNull()
        {
            var model = CreateValidModel();
            model.FullName = null!;

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.FullName)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenFullNameIsEmpty()
        {
            var model = CreateValidModel();
            model.FullName = "";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.FullName)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenFullNameIsWhitespace()
        {
            var model = CreateValidModel();
            model.FullName = "   ";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.FullName)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenPhoneIsNull()
        {
            var model = CreateValidModel();
            model.Phone = null!;

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Phone)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenPhoneIsEmpty()
        {
            var model = CreateValidModel();
            model.Phone = "";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Phone)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenPhoneIsWhitespace()
        {
            var model = CreateValidModel();
            model.Phone = "   ";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Phone)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenEmailIsNull()
        {
            var model = CreateValidModel();
            model.Email = null!;

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Email)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenEmailIsEmpty()
        {
            var model = CreateValidModel();
            model.Email = "";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Email)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenEmailIsWhitespace()
        {
            var model = CreateValidModel();
            model.Email = "   ";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Email)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenEmailFormatIsInvalid()
        {
            var model = CreateValidModel();
            model.Email = "invalid-email";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Email)));
        }

        [Theory]
        [InlineData("worker@test.com")]
        [InlineData("worker.name@test.com")]
        [InlineData("worker123@test.co.il")]
        public void WorkerRegisterDto_ShouldPass_WhenEmailFormatIsValid(string email)
        {
            var model = CreateValidModel();
            model.Email = email;

            var results = ValidateModel(model);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Email)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenMunicipalityIsNull()
        {
            var model = CreateValidModel();
            model.Municipality = null!;

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Municipality)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenMunicipalityIsEmpty()
        {
            var model = CreateValidModel();
            model.Municipality = "";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Municipality)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenMunicipalityIsWhitespace()
        {
            var model = CreateValidModel();
            model.Municipality = "   ";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Municipality)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenDepartmentIsNull()
        {
            var model = CreateValidModel();
            model.Department = null!;

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Department)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenDepartmentIsEmpty()
        {
            var model = CreateValidModel();
            model.Department = "";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Department)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenDepartmentIsWhitespace()
        {
            var model = CreateValidModel();
            model.Department = "   ";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Department)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenPasswordIsNull()
        {
            var model = CreateValidModel();
            model.Password = null!;

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Password)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenPasswordIsEmpty()
        {
            var model = CreateValidModel();
            model.Password = "";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Password)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenPasswordIsWhitespace()
        {
            var model = CreateValidModel();
            model.Password = "   ";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Password)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenPasswordIsTooShort()
        {
            var model = CreateValidModel();
            model.Password = "123";

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Password)));
        }

        [Theory]
        [InlineData("123456")]
        [InlineData("abcdef")]
        [InlineData("Abc12345")]
        public void WorkerRegisterDto_ShouldPass_WhenPasswordLengthIsValid(string password)
        {
            var model = CreateValidModel();
            model.Password = password;

            var results = ValidateModel(model);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Password)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldReturnMultipleErrors_WhenManyFieldsAreInvalid()
        {
            var model = new WorkerRegisterDto
            {
                NationalId = "",
                FullName = "",
                Phone = "",
                Email = "invalid",
                Municipality = "",
                Department = "",
                Password = "123"
            };

            var results = ValidateModel(model);

            Assert.True(results.Count >= 6);
        }

        [Fact]
        public void WorkerRegisterDto_ShouldFail_WhenAllFieldsAreNull()
        {
            var model = new WorkerRegisterDto
            {
                NationalId = null!,
                FullName = null!,
                Phone = null!,
                Email = null!,
                Municipality = null!,
                Department = null!,
                Password = null!
            };

            var results = ValidateModel(model);

            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.NationalId)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.FullName)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Phone)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Email)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Municipality)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Department)));
            Assert.Contains(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Password)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldKeepDepartmentValid_WhenUsingKnownDepartment()
        {
            var model = CreateValidModel();
            model.Department = "תאורה";

            var results = ValidateModel(model);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Department)));
        }

        [Fact]
        public void WorkerRegisterDto_ShouldKeepMunicipalityValid_WhenUsingKnownMunicipality()
        {
            var model = CreateValidModel();
            model.Municipality = "תל אביב-יפו";

            var results = ValidateModel(model);

            Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(WorkerRegisterDto.Municipality)));
        }
    }
}