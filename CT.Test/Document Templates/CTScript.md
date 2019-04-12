WordProcessor has many Document named Documents with container named ActiveDocuments  -- Composite w/ no model

Document is a Range

Document has:
* a model 
* an int key named Id
* a string named Name
* a DateTime named LastModified
* a TimeSpan named TotalEditingTime
* a PageSetup named PageSetup

PageSetup has a model -- Composite w/model
PageSetup has an Orientation named Orientation -- 
PageSetup has a MarginSetting named Margins
PageSetup has a PaperSize named Size

Orientation has a value 0 named Portrait -- enum
Orientation has a value 1 named Landscape

MarginSetting has a decimal named Top
MarginSetting has a decimal named Bottom
MarginSetting has a decimal named Left
MarginSetting has a decimal named Right

PaperSize has a decimal named Width
PaperSize has a decimal named Height

Range has many Range named Ranges with container named SubRanges
Range has a int key named Id
Range has a string named Text
Range has a Style named Style

Style has a bool named Bold
Style has a bool named Underline
Style has a bool named Italic
Style has a string named FontName
Style has an int named FontSize
Style has a string named FontColorName
Style has a string named HighlightColorName

^(?'container_or_derived_type'\D\w+)\s+(?'relationship'(has many)|(has\s+a?n?)|(is\s+a))\s*(?'contained_or_base_type'\D\S+)\s*(?'key_or_value'key|\d+|\s| |)(?:\s*named\s+)?(?'contained_type_name'\D\S+)?(?(?<=has many.+?)\s+with container named\s(?'container_name'.+)|$)

