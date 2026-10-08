using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Xunit;

namespace SmartPantry.ExternalProducts;

/// <summary>
/// Pruebas unitarias del AppService con un mock de IExternalProductCatalogClient.
/// No usan Internet ni HTTP: solo verifican cómo traduce el AppService cada situación.
/// </summary>
public class ExternalProductAppService_Tests
{
    private const string Barcode = "3017620422003";

    private readonly IExternalProductCatalogClient _client = Substitute.For<IExternalProductCatalogClient>();
    private readonly ExternalProductAppService _appService;

    public ExternalProductAppService_Tests()
    {
        _appService = new ExternalProductAppService(_client);
    }

    private static GetExternalProductInput Input(string barcode = Barcode) => new() { Barcode = barcode };

    [Fact]
    public async Task Should_Return_Found_With_Product_Data()
    {
        _client.GetByBarcodeAsync(Barcode).Returns(new ExternalProductDto
        {
            Barcode = Barcode,
            Name = "Nutella",
            Brand = "Ferrero",
            Quantity = "400 g",
            ImageUrl = "https://example.com/nutella.jpg"
        });

        var result = await _appService.GetByBarcodeAsync(Input());

        result.Status.ShouldBe(ExternalProductLookupStatus.Found);
        result.Barcode.ShouldBe(Barcode);
        result.Name.ShouldBe("Nutella");
        result.Brand.ShouldBe("Ferrero");
        result.Quantity.ShouldBe("400 g");
        result.ImageUrl.ShouldBe("https://example.com/nutella.jpg");
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Client_Returns_Null()
    {
        _client.GetByBarcodeAsync("0000000000000").Returns((ExternalProductDto?)null);

        var result = await _appService.GetByBarcodeAsync(Input("0000000000000"));

        result.Status.ShouldBe(ExternalProductLookupStatus.NotFound);
        result.Barcode.ShouldBe("0000000000000");
        result.Name.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_Found_Without_Inventing_Missing_Data()
    {
        _client.GetByBarcodeAsync(Barcode).Returns(new ExternalProductDto { Barcode = Barcode });

        var result = await _appService.GetByBarcodeAsync(Input());

        result.Status.ShouldBe(ExternalProductLookupStatus.Found);
        result.Name.ShouldBeNull();
        result.Brand.ShouldBeNull();
        result.Quantity.ShouldBeNull();
        result.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Return_RateLimited_When_Provider_Limits_Requests()
    {
        _client.GetByBarcodeAsync(Barcode)
            .Returns<Task<ExternalProductDto?>>(_ => throw new ExternalCatalogRateLimitException("429"));

        var result = await _appService.GetByBarcodeAsync(Input());

        result.Status.ShouldBe(ExternalProductLookupStatus.RateLimited);
        result.Barcode.ShouldBe(Barcode);
    }

    [Fact]
    public async Task Should_Return_ProviderUnavailable_When_Provider_Fails()
    {
        _client.GetByBarcodeAsync(Barcode)
            .Returns<Task<ExternalProductDto?>>(_ => throw new ExternalCatalogUnavailableException("timeout"));

        var result = await _appService.GetByBarcodeAsync(Input());

        result.Status.ShouldBe(ExternalProductLookupStatus.ProviderUnavailable);
        result.Barcode.ShouldBe(Barcode);
    }

    [Fact]
    public async Task Should_Trim_Barcode_Before_Calling_Client()
    {
        _client.GetByBarcodeAsync(Barcode).Returns((ExternalProductDto?)null);

        await _appService.GetByBarcodeAsync(Input($"  {Barcode}  "));

        await _client.Received(1).GetByBarcodeAsync(Barcode);
    }

    [Theory]
    [InlineData("3017620422003", true)]
    [InlineData("12345678", true)]
    [InlineData("1234567", false)]          // muy corto
    [InlineData("123456789012345", false)]  // muy largo
    [InlineData("30176204220AB", false)]    // no numérico
    [InlineData("", false)]
    public void Input_Dto_Should_Validate_Barcode(string barcode, bool expectedValid)
    {
        var input = Input(barcode);
        var results = new System.Collections.Generic.List<ValidationResult>();

        var isValid = Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);

        isValid.ShouldBe(expectedValid);
        if (!expectedValid)
        {
            results.Any().ShouldBeTrue();
        }
    }
}

