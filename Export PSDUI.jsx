// **************************************************
// Desc: 记录psd节点
// Author: ollve
// Date: 2020-06-19
//**************************************************
// hidden object game exporter
//$.writeln("=== Starting Debugging Session ===");

// enable double clicking from the Macintosh Finder or the Windows Explorer
#target
photoshop

// debug level: 0-2 (0:disable, 1:break on error, 2:break at beginning)
// $.level = 0;
// debugger; // launch debugger on next line

//储存当前文件
var savedRulerUnits
var savedTypeUnits
//字符串写入文件
var sceneData = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n";
//当前psd文件
var duppedPsd;
//文件夹名字
var destinationFolder;
//psd文件名字
var sourcePsdName;
//var parent
var curParent ="";
var recordParent ="";

main();

function main() {
    // got a valid document?
    if (app.documents.length <= 0) {
        if (app.playbackDisplayDialogs != DialogModes.NO) {
            alert("You must have a document open to export!");
        }
        // quit, returning 'cancel' makes the actions palette not record our script
        return 'cancel';
    }

    // ask for where the exported files should go
    destinationFolder = Folder.selectDialog("Choose the destination for export.");
    if (!destinationFolder) {
        return;
    }

    // cache useful variables
    sourcePsdName = app.activeDocument.name;
    var layerCount = app.documents[sourcePsdName].layers.length;
    var layerSetsCount = app.documents[sourcePsdName].layerSets.length;

    if ((layerCount <= 1) && (layerSetsCount <= 0)) {
        if (app.playbackDisplayDialogs != DialogModes.NO) {
            alert("You need a document with multiple layers to export!");
            // quit, returning 'cancel' makes the actions palette not record our script
            return 'cancel';
        }
    }

    // setup the units in case it isn't pixels
    savedRulerUnits = app.preferences.rulerUnits;
    savedTypeUnits = app.preferences.typeUnits;

    app.preferences.rulerUnits = Units.PIXELS;
    app.preferences.typeUnits = TypeUnits.PIXELS;

    // duplicate document so we can extract everythng we need
    duppedPsd = app.activeDocument.duplicate();
    
    // duppedPsd.activeLayer = duppedPsd.layers[duppedPsd.layers.length - 1];

    ControllCurrentPSD(duppedPsd,false);
    // export layers
    AddXmlHeadString();
    
    for(i = app.activeDocument.layers.length -1;i >=0;i--)
    {
        curParent ="";
        ExportLayerSet(app.activeDocument.layers[i]);
    }

    sceneData += "</layers>\n</PSDUI>";
    $.writeln(sceneData);

    duppedPsd.close(SaveOptions.DONOTSAVECHANGES);

    WriteXmlFile();
    // ControllCurrentPSD(duppedPsd,true)
}

function WriteXmlFile()
{
    //psd层布局文件信息保存
    var sceneFile = new File(destinationFolder + "/" + destinationFolder.name + ".xml");
    sceneFile.open('w');
    sceneFile.writeln(sceneData);
    sceneFile.close();

    app.preferences.rulerUnits = savedRulerUnits;
    app.preferences.typeUnits = savedTypeUnits;
}

function AddXmlHeadString()
{
    sceneData += "<PSDUI>\n";
    sceneData += "<psdSize>";
    sceneData += "<width>" + duppedPsd.width.value + "</width>";
    sceneData += "<height>" + duppedPsd.height.value + "</height>";
    sceneData += "</psdSize>\n";
    sceneData += "<layers>\n";
}

function ExportLayerSet(obj) {
    // alert("obj.name ="+obj.name + "- obj.typename =" + obj.typename);
    if (obj.typename == "ArtLayer")
    {
        if (obj.name.search("text") >= 0) 
        {
            ExportTextNew(obj);
        }else if (obj.name.search("label") >= 0) {
            ExportLabelNew(obj);
        } else if (obj.name.search("btn") >= 0) {
            ExportButtonNew(obj);
        } else {
            ExportImageNew(obj);
        }
    }else
    {
        curParent = recordParent;
        ExportNormalNew(obj);
    }

}


function ExportTextNew(obj)
{
    var strArray = obj.name.split("-")
    sceneData += ("<Layer><type>Text</type><name>" + strArray[0] + "</name>" + "<parent>" + curParent + "</parent>");
    SetLayerSizeAndPos(obj)

    AddTextArguments(obj)

    sceneData += "</Layer>\n";
}

function ExportLabelNew(obj)
{
    var strArray = obj.name.split("-")
    sceneData += ("<Layer><type>Label</type><name>" + strArray[0] + "</name>" + "<parent>" + curParent + "</parent>");
    SetLayerSizeAndPos(obj)

    AddTextArguments(obj)

    sceneData += "</Layer>\n";
}

function ContinueChild(obj)
{
    if(obj.layers.length > 0)
    {
        for (var i = obj.layers.length - 1; 0 <= i; i--) {

            ExportLayerSet(obj.layers[i])

        }
    }
}

function AddTextArguments(obj)
{
    sceneData += "<arguments>";
    sceneData += "<color>" + obj.textItem.color.rgb.hexValue + "</color>";
    sceneData += "<size>" + obj.textItem.size.value + "</size>";
    sceneData += "</arguments>";
}

function ExportButtonNew(obj)
{
    var strArray = obj.name.split("-")
    sceneData += ("<Layer><type>Button</type><name>" + strArray[0] + "</name>" + "<parent>" + curParent + "</parent>");
    SetLayerSizeAndPos(obj)

    sceneData += "</Layer>\n";
}

function ExportImageNew(obj)
{
    var strArray = obj.name.split("-")
    sceneData += ("<Layer><type>Image</type><name>" + strArray[0] + "</name>" + "<parent>" + curParent + "</parent>");
    SetLayerSizeAndPos(obj)

    sceneData += "</Layer>\n";
}

function ExportNormalNew(obj)
{
    sceneData += "<Layer>";
    sceneData += "<type>Normal</type>";
    sceneData += "<name>" + obj.name + "</name>" + "<parent>" + curParent + "</parent>";
    sceneData += "</Layer>\n";
    if(curParent == "")
    {
        curParent = obj.name;
        recordParent = obj.name;
    }
    else
    {
        curParent = curParent + "/" +obj.name;
    }
    ContinueChild(obj)
}

function ControllCurrentPSD(obj,isShow)
{
    if(obj.layers.length > 0)
    {
        for (var i = obj.layers.length - 1; 0 <= i; i--) {
            if (obj.layers[i].typename == "LayerSet") {
                return ControllCurrentPSD(obj.layers[i],isShow);
            } else {
                obj.layers[i].visible = isShow;
            }
        }
    }
}

function SetLayerSizeAndPos(layer)
{
    layer.visible = true;

    var recSize = GetLayerRec(duppedPsd.duplicate());

    sceneData += "<position>";
    sceneData += "<x>" + recSize.x + "</x>";
    sceneData += "<y>" + recSize.y + "</y>";
    sceneData += "</position>";

    sceneData += "<size>";
    sceneData += "<width>" + recSize.width + "</width>";
    sceneData += "<height>" + recSize.height + "</height>";
    sceneData += "</size>";

    layer.visible = false;
    
    return recSize;
}

function GetLayerRec(psd) 
{
    // we should now have a single art layer if all went well
    // psd.mergeVisibleLayers();
    // figure out where the top-left corner is so it can be exported into the scene file for placement in game
    // capture current size
    var height = psd.height.value;
    var width = psd.width.value;
    var top = psd.height.value;
    var left = psd.width.value;
    // trim off the top and left
    psd.trim(TrimType.TRANSPARENT, true, true, false, false);
    // the difference between original and trimmed is the amount of offset
    top -= psd.height.value;
    left -= psd.width.value;
    // trim the right and bottom
    psd.trim(TrimType.TRANSPARENT);
    // find center
    top += (psd.height.value / 2)
    left += (psd.width.value / 2)
    // unity needs center of image, not top left
    top = -(top - (height / 2));
    left -= (width / 2);

    height = psd.height.value;
    width = psd.width.value;

    psd.close(SaveOptions.DONOTSAVECHANGES);

    return {
        y: top,
        x: left,
        width: width,
        height: height
    };
}

