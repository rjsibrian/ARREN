-- =============================================
-- Author:		Meylin Vallecillos
-- Create date: 01/08/2016
-- Description:	Aumenta control de alertas en reporte
-- =============================================
CREATE PROCEDURE [dbo].[Sp_DbDatos_Arrendamiento_Pos_Alert_Load]
	-- Add the parameters for the stored procedure here

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	BEGIN TRY  
	 INSERT INTO [Sistemas].[Sistemas.Verify_Procedures_Use]
       ([Name])
	VALUES
       (OBJECT_NAME(@@PROCID))
	END TRY  
	BEGIN CATCH  
	    
	END CATCH;  

    -- Insert statements for procedure here
	UPDATE c
	SET 
		c.Alertas +=1 
	FROM 
		dbdatos.dbo.DBDATOS_ARRENDAMIENTO_COMERCIOS_TEMP as c
		Inner Join dbdatos.dbo.Vw_Arrendamiento_Pos_comercios as Vc on c.Retailer = Vc.Retailer
	WHERE 
		c.Estado > 0
		and Vc.debe > 1
END 