using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InventoryManagementSystem.Services
{
    public class LanguageService : INotifyPropertyChanged
    {
        private string _currentLanguage = "en"; // Default
        public string CurrentLanguage 
        { 
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Resources));
                }
            }
        }

        // Expose a dictionary binding for the UI (terminology overrides take precedence over the base language dictionary)
        public Dictionary<string, string> Resources
        {
            get
            {
                var merged = new Dictionary<string, string>(_dictionaries[_currentLanguage]);
                foreach (var kvp in _terminologyOverrides)
                {
                    merged[kvp.Key] = kvp.Value;
                }
                return merged;
            }
        }

        private readonly Dictionary<string, Dictionary<string, string>> _dictionaries = new()
        {
            ["en"] = new() 
            {
                ["Dashboard"] = "Dashboard",
                ["POS"] = "Checkout / Cashier",
                ["Inventory"] = "Products & Stock",
                ["Reports"] = "Reports",
                ["Insights"] = "Business Insights",
                ["Users"] = "Team Users",
                ["License"] = "License",
                ["Exit"] = "Exit Application",
                ["TotalRevenue"] = "Money from Sales",
                ["TotalProfit"] = "Profit",
                ["InventoryValue"] = "Stock Value",
                ["LowStockWarning"] = "Low Stock Warning",
                ["QuickActions"] = "Quick Actions",
                ["AddProduct"] = "Add Product",
                ["StockIN"] = "Add Stock",
                ["StockOUT"] = "Remove Stock",
                ["BulkImport"] = "Import Many",
                ["RecentMovements"] = "Recent Stock Changes",
                ["Welcome"] = "Welcome back, {0}",
                ["ItemsLow"] = "items are running low on stock. Check Products.",
                ["Login"] = "Login",
                ["Username"] = "Username",
                ["Password"] = "Password",
                ["LoginButton"] = "Login",
                // Inventory
                ["Inv_PaneTitle"] = "Manage Product",
                ["Inv_ProductName"] = "Product Name",
                ["Inv_SKU"] = "Product Code",
                ["Inv_Category"] = "Category",
                ["Inv_Unit"] = "Unit (e.g., Pcs, Kg)",
                ["Inv_InitialStock"] = "Starting Stock",
                ["Inv_MovementType"] = "Stock Change Type",
                ["Inv_Quantity"] = "Quantity",
                ["Inv_CostPerUnit"] = "What it cost you",
                ["Inv_SellingPrice"] = "Selling Price",
                ["Inv_Reason"] = "Reason",
                ["Inv_SearchPlaceholder"] = "Search products...",
                ["Inv_Import"] = "Import (CSV)",
                ["Inv_NewProduct"] = "+ New Product",
                ["Inv_Stock"] = "Stock",
                ["Inv_Actions"] = "Actions",
                ["Inv_Cancel"] = "Cancel",
                ["Inv_Save"] = "Save",

                // POS
                ["POS_Title"] = "Checkout",
                ["POS_Subtitle"] = "Select products to sell",
                ["POS_SearchPlaceholder"] = "Search by name or code...",
                ["POS_CurrentOrder"] = "Current Sale",
                ["POS_Items"] = "Items",
                ["POS_TotalAmount"] = "Total Amount",
                ["POS_AmountPaid"] = "Amount Paid",
                ["POS_ChangeDue"] = "Change to Give",
                ["POS_Checkout"] = "COMPLETE SALE",
                ["POS_PaymentSuccess"] = "Payment Successful!",
                ["POS_ReceiptDetails"] = "Receipt Details",
                ["POS_Close"] = "Close",
                ["POS_Print"] = "Print Receipt",

                // Settings
                ["Settings_Title"] = "System Settings",
                ["Settings_StoreInfo"] = "Store Information",
                ["Settings_StoreInfoDesc"] = "These details will appear on printed receipts.",
                ["Settings_StoreName"] = "Store Name",
                ["Settings_Address"] = "Address",
                ["Settings_Currency"] = "Currency",
                ["Settings_Printer"] = "Printer Name",
                ["Settings_Save"] = "Save Settings",

                // Reports
                ["Rep_Title"] = "Reports & Insights",
                ["Rep_Subtitle"] = "See how your business is doing",
                ["Rep_Sales"] = "Sales Report",
                ["Rep_InvValue"] = "Stock Value",
                ["Rep_LowStock"] = "Low Stock",
                ["Rep_Profit"] = "Income vs Expenses",
                ["Rep_Generate"] = "Generate Report",
                ["Rep_ExportPDF"] = "Export PDF",
                // Sidebar
                ["Customers"] = "Customers",
                ["Suppliers"] = "Suppliers",
                ["Employees"] = "Staff",
                ["PurchaseOrders"] = "Orders to Suppliers",
                ["Forecasting"] = "Demand Forecast",
                ["ReorderDashboard"] = "What to Reorder",
                ["ExpiryDashboard"] = "Expiring Stock",
                ["Locations"] = "Store Locations",
                ["Returns"] = "Returns & Refunds",
                ["Bundles"] = "Product Bundles",
                ["AuditTrail"] = "Activity Log",
                ["AdvancedInsights"] = "Advanced Insights",
                ["Settings"] = "Settings",
                ["Manufacturing"] = "Manufacturing",
                ["AccountsPayable"] = "Accounts Payable",
                ["Attendance"] = "Attendance & Leave",
                ["Enterprise"] = "Enterprise",
                ["Expenses"] = "Expenses",
                ["Accounting"] = "Accounting",
                ["Invoices"] = "Customer Invoices",
                ["Bills"] = "Vendor Bills",
                ["ChartOfAccounts"] = "Chart of Accounts",
                ["FinancialReports"] = "Financial Reports",
                ["ToggleTheme"] = "Toggle Theme",
                ["SwitchUser"] = "Switch User",
                ["CheckUpdates"] = "Check Updates",
                ["Nav_Main"] = "MAIN",
                ["Nav_Operations"] = "OPERATIONS",
                ["Nav_Finance"] = "RELATIONSHIPS & FINANCE",
                ["Nav_People"] = "PEOPLE & TEAMS",
                ["Nav_Analytics"] = "ANALYTICS & AUDIT",
                ["Nav_System"] = "SYSTEM",
                // Everyday business terms (plain English defaults)
                ["Customer"] = "Customer",
                ["Category"] = "Category",
                ["Product"] = "Product",
                ["Supplier"] = "Supplier",
                ["Order"] = "Order",
                ["Invoice"] = "Customer Bill",
                ["SalesOrder"] = "Customer Order",
                ["SalesQuotation"] = "Price Quote",
                ["PurchaseOrder"] = "Buy Order",
                ["Rfq"] = "Ask Supplier for Price",
                ["CreditNote"] = "Customer Refund Credit",
                ["DebitNote"] = "Supplier Refund",
                ["DeliveryNote"] = "Delivery Slip",
                ["PackingSlip"] = "Packing List",
                ["AccountsReceivable"] = "Money Customers Owe You",
                ["AccountsPayable"] = "Money You Owe Suppliers",
                ["Vat"] = "Sales Tax",
                ["Cogs"] = "Cost of Stock Sold",
                ["Journal"] = "Money Record",
                ["ChartOfAccounts"] = "Money Categories",
                ["BankReconciliation"] = "Match Bank to Payments",
                ["RecurringInvoice"] = "Repeat Bill",
                ["ConfirmShipment"] = "Mark as Sent",
                ["BillingStatus"] = "Payment Status"
            },
            ["fr"] = new()
            {
                ["Dashboard"] = "Tableau de bord",
                ["POS"] = "Caisse / PV",
                ["Inventory"] = "Produits",
                ["Reports"] = "Rapports",
                ["Insights"] = "Analyses",
                ["Users"] = "Utilisateurs",
                ["License"] = "Licence",
                ["Exit"] = "Quitter",
                ["TotalRevenue"] = "Revenu Total",
                ["TotalProfit"] = "Benefice Total",
                ["InventoryValue"] = "Valeur Inventaire",
                ["LowStockWarning"] = "Alerte Stock Faible",
                ["QuickActions"] = "Actions Rapides",
                ["AddProduct"] = "Ajouter Produit",
                ["StockIN"] = "Entree Stock",
                ["StockOUT"] = "Sortie Stock",
                ["BulkImport"] = "Import Global",
                ["RecentMovements"] = "Mouvements Recents",
                ["Welcome"] = "Bienvenue, {0}",
                ["ItemsLow"] = "articles sont en rupture de stock. Verifier l'inventaire.",
                ["Login"] = "Connexion",
                ["Username"] = "Nom d'utilisateur",
                ["Password"] = "Mot de passe",
                ["LoginButton"] = "Se connecter",

                // Inventory
                ["Inv_PaneTitle"] = "Gerer Produit",
                ["Inv_ProductName"] = "Nom du Produit",
                ["Inv_SKU"] = "SKU",
                ["Inv_Category"] = "Categorie",
                ["Inv_Unit"] = "Unite (ex: Pcs, Kg)",
                ["Inv_InitialStock"] = "Stock Initial",
                ["Inv_MovementType"] = "Type de Mouvement",
                ["Inv_Quantity"] = "Quantite",
                ["Inv_CostPerUnit"] = "Cout unitaire",
                ["Inv_SellingPrice"] = "Prix de Vente",
                ["Inv_Reason"] = "Raison",
                ["Inv_SearchPlaceholder"] = "Rechercher...",
                ["Inv_Import"] = "Importer (CSV)",
                ["Inv_NewProduct"] = "+ Nouveau Produit",
                ["Inv_Stock"] = "Stock",
                ["Inv_Actions"] = "Actions",
                ["Inv_Cancel"] = "Annuler",
                ["Inv_Save"] = "Enregistrer",

                // POS
                ["POS_Title"] = "Point de Vente",
                ["POS_Subtitle"] = "Selectionner produits",
                ["POS_SearchPlaceholder"] = "Recherche par Nom ou SKU...",
                ["POS_CurrentOrder"] = "Commande Actuelle",
                ["POS_Items"] = "Articles",
                ["POS_TotalAmount"] = "Montant Total",
                ["POS_AmountPaid"] = "Montant Paye",
                ["POS_ChangeDue"] = "Monnaie a Rendre",
                ["POS_Checkout"] = "TERMINER VENTE",
                ["POS_PaymentSuccess"] = "Paiement Reussi!",
                ["POS_ReceiptDetails"] = "Details du Recu",
                ["POS_Close"] = "Fermer",
                ["POS_Print"] = "Imprimer Recu",

                // Settings
                ["Settings_Title"] = "Parametres Systeme",
                ["Settings_StoreInfo"] = "Information Magasin",
                ["Settings_StoreInfoDesc"] = "Ces details apparaitront sur les recus.",
                ["Settings_StoreName"] = "Nom du Magasin",
                ["Settings_Address"] = "Adresse",
                ["Settings_Currency"] = "Devise",
                ["Settings_Printer"] = "Nom Imprimante",
                ["Settings_Save"] = "Enregistrer Parametres",

                // Reports
                ["Rep_Title"] = "Rapports & Analyses",
                ["Rep_Subtitle"] = "Generer rapports financiers",
                ["Rep_Sales"] = "Rapport Ventes",
                ["Rep_InvValue"] = "Val. Inventaire",
                ["Rep_LowStock"] = "Stock Faible",
                ["Rep_Profit"] = "Pertes & Profits",
                ["Rep_Generate"] = "Generer Rapport",
                ["Rep_ExportPDF"] = "Exporter PDF",
                // Sidebar
                ["Customers"] = "Clients",
                ["Suppliers"] = "Fournisseurs",
                ["Employees"] = "Personnel",
                ["PurchaseOrders"] = "Bons de Commande",
                ["Forecasting"] = "Previsions",
                ["ReorderDashboard"] = "Tableau Reapprov.",
                ["ExpiryDashboard"] = "Tableau Expiration",
                ["Locations"] = "Emplacements",
                ["Returns"] = "Retours",
                ["Bundles"] = "Kitting & Bundles",
                ["AuditTrail"] = "Audit System",
                ["AdvancedInsights"] = "Analyses Avancees",
                ["Settings"] = "Parametres",
                ["Manufacturing"] = "Fabrication",
                ["AccountsPayable"] = "Dettes Fournisseurs",
                ["Attendance"] = "Presence & Conges",
                ["Enterprise"] = "Entreprise",
                ["Expenses"] = "Depenses",
                ["Accounting"] = "Comptabilité",
                ["Invoices"] = "Factures Clients",
                ["Bills"] = "Factures Fournisseurs",
                ["ChartOfAccounts"] = "Plan Comptable",
                ["FinancialReports"] = "Rapports Financiers",
                ["ToggleTheme"] = "Changer Theme",
                ["SwitchUser"] = "Changer Utilisateur",
                ["CheckUpdates"] = "Mises a jour",
                ["Nav_Main"] = "PRINCIPAL",
                ["Nav_Operations"] = "OPERATIONS",
                ["Nav_Finance"] = "FINANCES & VENTES",
                ["Nav_People"] = "EQUIPE & RH",
                ["Nav_Analytics"] = "ANALYSES & AUDIT",
                ["Nav_System"] = "SYSTEME",
                // Terminology-overridable generic terms
                ["Customer"] = "Client",
                ["Category"] = "Categorie",
                ["Product"] = "Produit"
            },
            ["rw"] = new()
            {
                ["Dashboard"] = "Ikibaho (Dashboard)",
                ["POS"] = "Ahagurirwa (POS)",
                ["Inventory"] = "Ibicuruzwa",
                ["Reports"] = "Raporo",
                ["Insights"] = "Isesengura",
                ["Users"] = "Abakozi",
                ["License"] = "Uruhushya",
                ["Exit"] = "Funga Porogaramu",
                ["TotalRevenue"] = "Amafaranga Yinjiye",
                ["TotalProfit"] = "Inyungu",
                ["InventoryValue"] = "Agaciro k'Ibicuruzwa",
                ["LowStockWarning"] = "Ibicuruzwa Byabaye Bike",
                ["QuickActions"] = "Ibikorwa Byihuse",
                ["AddProduct"] = "Ongeramo Igicuruzwa",
                ["StockIN"] = "Kwinjiza/Kurangura",
                ["StockOUT"] = "Gusohora/Kugurisha",
                ["BulkImport"] = "Injiza Byinshi (Import)",
                ["RecentMovements"] = "Ibyahindutse Vuba",
                ["Welcome"] = "Murakaza neza, {0}",
                ["ItemsLow"] = "byabaye bike cyane mu bubiko. Reba Ububiko.",
                ["Login"] = "Injira",
                ["Username"] = "Izina",
                ["Password"] = "Ijambo ry'ibanga",
                ["LoginButton"] = "Injira",

                // Inventory
                ["Inv_PaneTitle"] = "Hindura Igicuruzwa",
                ["Inv_ProductName"] = "Izina ry'igicuruzwa",
                ["Inv_SKU"] = "SKU (Kode)",
                ["Inv_Category"] = "Icyiciro",
                ["Inv_Unit"] = "Urugero (urugero: Pcs, Kg)",
                ["Inv_InitialStock"] = "Ingano itangira",
                ["Inv_MovementType"] = "Ubwoko bw'igikorwa",
                ["Inv_Quantity"] = "Ingano",
                ["Inv_CostPerUnit"] = "Igiciro cyo kurangura",
                ["Inv_SellingPrice"] = "Igiciro cyo kugurisha",
                ["Inv_Reason"] = "Impamvu",
                ["Inv_SearchPlaceholder"] = "Shakisha...",
                ["Inv_Import"] = "Injiza (CSV)",
                ["Inv_NewProduct"] = "+ Igicuruzwa Gishya",
                ["Inv_Stock"] = "Ububiko",
                ["Inv_Actions"] = "Ibikorwa",
                ["Inv_Cancel"] = "Bureka",
                ["Inv_Save"] = "Bika",

                // POS
                ["POS_Title"] = "Aho bagurishiriza",
                ["POS_Subtitle"] = "Hitamo ibicuruzwa",
                ["POS_SearchPlaceholder"] = "Shaka Izina cyangwa SKU...",
                ["POS_CurrentOrder"] = "Ibigurwa",
                ["POS_Items"] = "Ibicuruzwa",
                ["POS_TotalAmount"] = "Yose Hamwe",
                ["POS_AmountPaid"] = "Ayishyuwe",
                ["POS_ChangeDue"] = "Agarurwa",
                ["POS_Checkout"] = "SOZA KUGURISHA",
                ["POS_PaymentSuccess"] = "Kwishyura byagenze neza!",
                ["POS_ReceiptDetails"] = "Imiterere ya Risiti",
                ["POS_Close"] = "Funga",
                ["POS_Print"] = "Sohora Risiti",

                // Settings
                ["Settings_Title"] = "Igenamiterere",
                ["Settings_StoreInfo"] = "Amakuru y'Iduka",
                ["Settings_StoreInfoDesc"] = "Ibi bizagaragara kuri risiti.",
                ["Settings_StoreName"] = "Izina ry'Iduka",
                ["Settings_Address"] = "Aderesi",
                ["Settings_Currency"] = "Ifaranga",
                ["Settings_Printer"] = "Izina rya Printer",
                ["Settings_Save"] = "Bika Igenamiterere",

                // Reports
                ["Rep_Title"] = "Raporo & Isesengura",
                ["Rep_Subtitle"] = "Reba raporo z'imari n'ububiko",
                ["Rep_Sales"] = "Raporo y'ibwaguzwe",
                ["Rep_InvValue"] = "Agaciro k'ububiko",
                ["Rep_LowStock"] = "Ibike mu bubiko",
                ["Rep_Profit"] = "Inyungu & Igihombo",
                ["Rep_Generate"] = "Kora Raporo",
                ["Rep_ExportPDF"] = "Bika nka PDF",
                // Sidebar
                ["Customers"] = "Abakiriya",
                ["Suppliers"] = "Abasupplier",
                ["Employees"] = "Abakozi",
                ["PurchaseOrders"] = "Bons de Commande",
                ["Forecasting"] = "Ibibanziriza Igihe",
                ["ReorderDashboard"] = "Guhindura Ububiko",
                ["ExpiryDashboard"] = "Ibirangiriza Igihe",
                ["Locations"] = "Ahari Ububiko",
                ["Returns"] = "Ibiregarwa",
                ["Bundles"] = "Ibicuruzwa Bivanzwe",
                ["AuditTrail"] = "Imicungire y'Ububiko",
                ["AdvancedInsights"] = "Isesengura Ryimbitse",
                ["Settings"] = "Igenamiterere",
                ["Manufacturing"] = "Gukora (Mfg)",
                ["AccountsPayable"] = "Imyenda y'Abagemuzi",
                ["Attendance"] = "Ubwitabire & Konje",
                ["Enterprise"] = "Ikigo",
                ["Expenses"] = "Amafaranga Yakoreshejwe",
                ["Accounting"] = "Ibaruramari",
                ["Invoices"] = "Inyemezabwishyu z'Abakiriya",
                ["Bills"] = "Inyemezabwishyu z'Abarangura",
                ["ChartOfAccounts"] = "Ibyiciro by'Imari",
                ["FinancialReports"] = "Raporo z'Imari",
                ["ToggleTheme"] = "Hindura Ibara",
                ["SwitchUser"] = "Hindura Umukoresha",
                ["CheckUpdates"] = "Amavugurura",
                ["Nav_Main"] = "IBY'INGENZI",
                ["Nav_Operations"] = "IBIKORWA",
                ["Nav_Finance"] = "IMARI N'UBURUNZI",
                ["Nav_People"] = "ABAKOZI",
                ["Nav_Analytics"] = "ISESENGURA",
                ["Nav_System"] = "SISITEMU",
                // Terminology-overridable generic terms
                ["Customer"] = "Umukiriya",
                ["Category"] = "Icyiciro",
                ["Product"] = "Igicuruzwa"
            }
        };

        private Dictionary<string, string> _terminologyOverrides = new();

        public void SetTerminologyOverrides(Dictionary<string, string> overrides)
        {
            _terminologyOverrides = overrides ?? new();
            OnPropertyChanged(nameof(Resources));
        }

        public void SetLanguage(string code)
        {
            if (_dictionaries.ContainsKey(code))
            {
                CurrentLanguage = code;
            }
        }

        public string GetString(string key)
        {
            return Resources.TryGetValue(key, out var value) ? value : key; // Fallback to key itself
        }
        
        // Dynamic property access for Binding: {Binding Language.Res[Key]}
        public string this[string key] => GetString(key);

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
