-- =============================================
-- Author:		Meylin Vallecillos	
-- Create date: 24/01/2020
-- Description:	Devuelve lista de comercios con arrendamiento para actualizar campo
-- =============================================
CREATE PROCEDURE [Comercios].[Sp_Arrendamiento_Lista_gete]
	-- Add the parameters for the stored procedure here
	@Fecha date
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT 
		 b.Retailer
		,a.Retailer Padre
		,b.Consolidar
		,sum(Monto_Arrendamiento) Monto
		,count(t.Terminal) Cantidad
	FROM 
		[dbdatos].[Comercios].[DbDatos_Terminals] t
		INNER JOIN [dbdatos].[Comercios].[DbDatos_Bussiness_Data] b on t.IdBussiness_Fk = b.Id
		INNER JOIN [dbdatos].[Comercios].[DbDatos_Afiliation_Data] a on b.IdAfiliacion_Fk = a.Id
	WHERE 
		Monto_Arrendamiento > 0 
		and t.Status ='0'	
		and (t.FechaInicio < @Fecha or t.Fecha_Inicio < @Fecha)
	GROUP BY 
		b.Retailer
		,a.Retailer
		,b.Consolidar
END 