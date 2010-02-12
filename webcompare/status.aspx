<%@ Page Language="C#" AutoEventWireup="false" Inherits="WebCompare.StatusPage"%>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.Collections.Specialized" %>
<%@ Import Namespace="GuiCompare" %>
<html>
<head id="head1" runat="server">
<title>Mono API Compare</title>
<link href="main.css" media="screen" type="text/css" rel="stylesheet">
</head>
<body>
    <div id="header">
    	<h1 runat="server" id="page_header" EnableViewState="false">Mono Class Status Pages</h1>
    </div>
    <form id="form" runat="server">
	<div id="content">
		<div id="treeview">
			<br>
			<asp:Label id="time_label" runat="server"/>
			<asp:TreeView ID="tree" Runat="server"
				OnTreeNodePopulate="TreeNodePopulate"
				EnableClientScript="true"
				PopulateNodesFromClient="true"
				ExpandDepth="1">
			</asp:TreeView>
		</div>
		<div id="legend">
			<asp:ListView runat="server" id="tbl_legend" EnableViewState="false">
			<LayoutTemplate>
				<table border="0" cellpadding="0" cellspacing="0">
				<caption style="background-color: #4c83c4; color: #fff"><b>Legend<b></caption>
					<asp:PlaceHolder runat="server" id="itemPlaceholder" />
				</table>
			</LayoutTemplate>
			<ItemTemplate>
				<tr class="item">
				<td><img src='<%# DataBinder.Eval (Container.DataItem, "Image") %>'>
				</td>
				<td style="text-align: left; padding-left: 7px;"><%# DataBinder.Eval (Container.DataItem, "Description") %>
				</td>
				</tr>
			</ItemTemplate>
			<AlternatingItemTemplate>
				<tr class="item alternatingitem">
				<td><img src='<%# DataBinder.Eval (Container.DataItem, "Image") %>'>
				</td>
				<td style="text-align: left; padding-left: 7px;"><%# DataBinder.Eval (Container.DataItem, "Description") %>
				</td>
				</tr>
				</AlternatingItemTemplate>
			</asp:ListView>
		</div>

	</div>
    </form>
</body>
</html>
