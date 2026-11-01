<%@ Page Language="C#" Async="true" AutoEventWireup="true" CodeBehind="Aadhar_Scanner.aspx.cs" Inherits="Aadhar_Card_Scanner.Aadhar_Scanner" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Aadhaar Card Scanner</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <style>
        * {
            box-sizing: border-box;
        }

        body {
            margin: 0;
            padding: 30px 15px;
            font-family: Arial, sans-serif;
            background: #f1f5f9;
            color: #1e293b;
        }

        .container {
            max-width: 950px;
            margin: auto;
            background: white;
            padding: 30px;
            border-radius: 15px;
            box-shadow: 0 5px 25px rgba(0,0,0,.08);
        }

        h1 {
            text-align: center;
            margin-bottom: 10px;
            color: #123c69;
        }

        .subtitle {
            text-align: center;
            color: #64748b;
            margin-bottom: 30px;
        }

        .upload-grid {
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 20px;
        }

        .upload-box {
            border: 2px dashed #94a3b8;
            border-radius: 12px;
            padding: 22px;
            text-align: center;
            background: #f8fafc;
        }

            .upload-box h3 {
                margin-top: 0;
            }

        input[type=file] {
            width: 100%;
            margin: 15px 0;
        }

        .preview {
            width: 100%;
            max-height: 230px;
            object-fit: contain;
            display: none;
            margin-top: 10px;
            border-radius: 8px;
            background: #e2e8f0;
        }

        .scan-button {
            display: block;
            width: 100%;
            margin-top: 25px;
            padding: 15px;
            border: none;
            border-radius: 8px;
            background: #123c69;
            color: white;
            font-size: 16px;
            font-weight: bold;
            cursor: pointer;
        }

            .scan-button:hover {
                background: #1d568e;
            }

        .results {
            margin-top: 30px;
            padding: 22px;
            border: 1px solid #cbd5e1;
            border-radius: 12px;
        }

            .results h2 {
                margin-top: 0;
                color: #123c69;
            }

        .field {
            margin-bottom: 18px;
        }

            .field label {
                display: block;
                margin-bottom: 7px;
                font-weight: bold;
            }

            .field input, .field textarea {
                width: 100%;
                padding: 12px;
                border: 1px solid #cbd5e1;
                border-radius: 7px;
                font-size: 15px;
            }

            .field textarea {
                min-height: 100px;
                resize: vertical;
            }

        .message {
            display: block;
            margin-top: 18px;
            color: #b91c1c;
            overflow-wrap: anywhere;
        }

        .privacy {
            margin-top: 20px;
            padding: 12px;
            background: #fff7ed;
            border-radius: 8px;
            color: #9a3412;
            font-size: 13px;
            line-height: 1.6;
        }

        @media(max-width: 650px) {
            .upload-grid {
                grid-template-columns: 1fr;
            }

            .container {
                padding: 20px;
            }
        }
    </style>

    <script type="text/javascript">

/**
 * @param {HTMLInputElement} input
 * @param {string} imageId
 */

function previewImage(input, imageId) {

    /** @type {HTMLImageElement | null} */
    var image = /** @type {HTMLImageElement | null} */
        (document.getElementById(imageId));

    if (image == null) {
        return;
    }

    if (input.files && input.files.length > 0) {

        var file = input.files[0];

        // Validate image

        if (!["image/jpeg", "image/png"].includes(file.type)) {

            alert("Please select a JPG, JPEG or PNG image.");

            input.value = "";

            image.style.display = "none";
            image.removeAttribute("src");

            return;
        }

        // Remove previous image preview

        if (image.dataset.previewUrl) {
            URL.revokeObjectURL(image.dataset.previewUrl);
        }

        // Display preview

        var previewUrl = URL.createObjectURL(file);

        image.dataset.previewUrl = previewUrl;

        image.src = previewUrl;
        image.style.display = "block";

    }
    else {

        if (image.dataset.previewUrl) {

            URL.revokeObjectURL(image.dataset.previewUrl);

            delete image.dataset.previewUrl;
        }

        image.style.display = "none";
        image.removeAttribute("src");

    }

}

</script>
</head>
<body>
    <form id="form1" runat="server" enctype="multipart/form-data">
        <div class="container">
            <h1>Aadhaar Card Scanner</h1>
            <p class="subtitle">Upload document images and extract text automatically. </p>
            <div class="upload-grid">
                <div class="upload-box">
                    <h3>Front Side</h3>
                    <asp:FileUpload ID="fuFront" runat="server" accept=".jpg,.jpeg,.png" onchange="previewImage(this,'imgFront')" />
                    <img src="data:image/gif;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=" id="imgFront" class="preview" alt="Front image preview" />
                </div>
                <div class="upload-box">
                    <h3>Back Side</h3>
                    <asp:FileUpload ID="fuBack" runat="server" accept=".jpg,.jpeg,.png" onchange="previewImage(this,'imgBack')" />
                    <img src="data:image/gif;base64,R0lGODlhAQABAAD/ACwAAAAAAQABAAACADs=" id="imgBack" class="preview" alt="Back image preview" />
                </div>
            </div>
            <asp:Button
                ID="btnScan"
                runat="server"
                Text="SCAN IMAGES"
                CssClass="scan-button"
                OnClick="btnScan_Click" />

            <asp:Label ID="lblMessage" runat="server" CssClass="message" />
            <asp:Panel ID="pnlResults" runat="server" CssClass="results" Visible="false">
                <h2>Extracted Details</h2>
                <div class="field">

                    <label>Full Name</label>
                    <asp:TextBox ID="txtName" runat="server" />
                </div>
                <div class="field">
                    <label>Date of Birth</label>
                    <asp:TextBox ID="txtDOB" runat="server" />
                </div>

                <div class="field">
                    <label>Address</label>
                    <asp:TextBox ID="txtAddress" runat="server" TextMode="MultiLine" />
                </div>
            </asp:Panel>
            <div class="privacy"><strong>Privacy:</strong> Use synthetic test documents during development. Uploaded identity images contain sensitive personal information. This demonstration does not verify Aadhaar authenticity. </div>
        </div>
    </form>
</body>
</html>
