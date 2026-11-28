# Enum BarcodeType

Namespace: [TerraFluent.Barcode](index.md)  
Assembly: TerraFluent.Barcode.dll  

The barcode symbologies supported by TerraFluent.Barcode.

```csharp
public enum BarcodeType
```

## Fields

<a id="TerraFluent_Barcode_BarcodeType_Code128"></a>`Code128 = 0` 

Code 128 (ISO/IEC 15417): full ASCII, with automatic code set A/B/C switching.

<a id="TerraFluent_Barcode_BarcodeType_Code39"></a>`Code39 = 1` 

Code 39 (ISO/IEC 16388): digits, upper-case letters and <code>- . $ / + %</code> and space.

<a id="TerraFluent_Barcode_BarcodeType_Ean13"></a>`Ean13 = 2` 

EAN-13 (ISO/IEC 15420): 12 digits plus a check digit.

<a id="TerraFluent_Barcode_BarcodeType_Ean8"></a>`Ean8 = 3` 

EAN-8 (ISO/IEC 15420): 7 digits plus a check digit.

<a id="TerraFluent_Barcode_BarcodeType_UpcA"></a>`UpcA = 4` 

UPC-A (ISO/IEC 15420): 11 digits plus a check digit.

<a id="TerraFluent_Barcode_BarcodeType_QrCode"></a>`QrCode = 5` 

QR code (ISO/IEC 18004), model 2, versions 1-40.

<a id="TerraFluent_Barcode_BarcodeType_Gs1Code128"></a>`Gs1Code128 = 6` 

GS1-128 (Code 128 with FNC1): GS1 application identifiers and data for logistics labels.

<a id="TerraFluent_Barcode_BarcodeType_DataMatrix"></a>`DataMatrix = 7` 

Data Matrix ECC 200 (ISO/IEC 16022): compact 2D code, square or rectangular.

<a id="TerraFluent_Barcode_BarcodeType_Pdf417"></a>`Pdf417 = 8` 

PDF417 (ISO/IEC 15438): stacked 2D code used on ID cards, boarding passes and shipping labels.

<a id="TerraFluent_Barcode_BarcodeType_Itf"></a>`Itf = 9` 

Interleaved 2 of 5 (ISO/IEC 16390): an even number of digits, with an optional modulo 10 check digit.

<a id="TerraFluent_Barcode_BarcodeType_Itf14"></a>`Itf14 = 10` 

ITF-14 (GS1): Interleaved 2 of 5 for a 14-digit GTIN on cartons and outer packaging.

<a id="TerraFluent_Barcode_BarcodeType_Codabar"></a>`Codabar = 11` 

Codabar (NW-7): digits and <code>- $ : / . +</code> framed by start/stop characters A to D; used by libraries and blood banks.

<a id="TerraFluent_Barcode_BarcodeType_Aztec"></a>`Aztec = 12` 

Aztec Code (ISO/IEC 24778): compact 2D code with a central bullseye that needs no quiet zone; used on transport tickets.

<a id="TerraFluent_Barcode_BarcodeType_MaxiCode"></a>`MaxiCode = 13` 

MaxiCode (ISO/IEC 16023): fixed-size 2D code of hexagons around a bullseye, used for parcel sorting.

<a id="TerraFluent_Barcode_BarcodeType_MicroQrCode"></a>`MicroQrCode = 14` 

Micro QR code (ISO/IEC 18004), versions M1 to M4: one finder pattern, 11 to 17 modules square, for small items.

<a id="TerraFluent_Barcode_BarcodeType_RectangularMicroQrCode"></a>`RectangularMicroQrCode = 15` 

Rectangular Micro QR code (rMQR, ISO/IEC 23941): 7 to 17 modules high and up to 139 wide, for narrow print areas.

<a id="TerraFluent_Barcode_BarcodeType_Gs1DataBar"></a>`Gs1DataBar = 16` 

GS1 DataBar Omnidirectional (ISO/IEC 24724): a GTIN-14 in a short symbol, read in any direction at retail checkouts.

<a id="TerraFluent_Barcode_BarcodeType_Gs1DataBarLimited"></a>`Gs1DataBarLimited = 17` 

GS1 DataBar Limited (ISO/IEC 24724): a GTIN-14 starting with 0 or 1 in the narrowest DataBar, for small items.

<a id="TerraFluent_Barcode_BarcodeType_Gs1DataBarExpanded"></a>`Gs1DataBarExpanded = 18` 

GS1 DataBar Expanded (ISO/IEC 24724): GS1 application identifiers and data, such as weight, price and dates, for fresh foods and coupons.

<a id="TerraFluent_Barcode_BarcodeType_IntelligentMail"></a>`IntelligentMail = 19` 

USPS Intelligent Mail barcode (USPS-B-3200): 65 four-state bars carrying a 20-digit tracking code and a ZIP code routing code.
