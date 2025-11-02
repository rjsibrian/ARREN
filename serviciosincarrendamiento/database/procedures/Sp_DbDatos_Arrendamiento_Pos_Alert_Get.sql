-- =============================================
-- Author:		Meylin Vallecillos
-- Create date: 23/08/2016
-- Description:	Retailers con morosidad
-- =============================================
CREATE PROCEDURE [dbo].[Sp_DbDatos_Arrendamiento_Pos_Alert_Get]
	-- Add the parameters for the stored procedure here
	  @Tipo int
	, @Desde datetime
	, @Hasta datetime
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
	
--	select 
--		  c.[retailer]
--		, c.[monto]
--		, c.[inicio]
--		, c.[saldo] - c.[monto] as saldo
--		, c.[debe] -1 as debe
--		, c.[mes]
--		, c.[pos]
--		, case when (c.[estado] = 1) then 'Activo' else 'Inactivo' end as estado
--		, COALESCE((select sum(monto) from dbdatos.dbo.Vw_DbDatos_Abonos where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0) as abonos
--		, COALESCE((select sum(debitoContracargos) from dbdatos.dbo.Vw_DbDatos_Abonos where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0) as debito_c
--		, COALESCE((select sum(debitoArrendamiento) from dbdatos.dbo.Vw_DbDatos_Abonos where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0) as debito_a
--		, COALESCE((select max(monto) from dbdatos.dbo.Vw_DbDatos_Abonos where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0)  as maximo
--	from dbdatos.[dbo].[Vw_Arrendamiento_Pos_comercios] as c 
--	WHERE 
--			   (@Tipo = 1 AND c.debe < 2 AND c.estado = 1)
--			OR (@Tipo = 2 AND c.debe > 1 AND c.estado = 1)
--		ORDER BY
--			c.debe


			
	select 
		  c.[retailer]
		  ,b.Nombre [nombre]
		,i.Nombre [banco]
		, c.[monto]
		, c.[inicio]
		, c.[saldo] - c.[monto] as saldo
		, c.[debe] -1 as debe
		, c.[mes]
		, c.[pos]
		, case when (c.[estado] = 1) then 'Activo' else 'Inactivo' end as estado
		, coalesce((select sum(monto) from (SELECT        d.rtl_id AS retailer, d.id_pag AS idPago, d.monto, COALESCE(dc.Monto, 0) AS debitoContracargos, COALESCE(SUM(a.Monto_Debitar), 0) AS debitoArrendamiento, 
                         CONVERT(date, d.fecha_gen) AS fecha
FROM            dbo.dftdet AS d LEFT OUTER JOIN
                         dbo.DbDatos_Debitos_Contracargos AS dc ON d.id_pag = dc.IdPago LEFT OUTER JOIN
                         dbo.DbDatos_Arrendamiento_Acum AS a ON d.rtl_id = a.Retailer AND d.id_pag = a.IdPago
WHERE        (d.estado = 0 and d.post_dat between  convert(varchar(6), @desde, 12) and convert(varchar(6), @hasta, 12))
GROUP BY d.rtl_id, d.id_pag, d.monto, dc.Monto, d.fecha_gen)a where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0) as abonos
		, coalesce((select sum(debitoContracargos) from (SELECT        d.rtl_id AS retailer, d.id_pag AS idPago, d.monto, COALESCE(dc.Monto, 0) AS debitoContracargos, COALESCE(SUM(a.Monto_Debitar), 0) AS debitoArrendamiento, 
                         CONVERT(date, d.fecha_gen) AS fecha
FROM            dbo.dftdet AS d LEFT OUTER JOIN
                         dbo.DbDatos_Debitos_Contracargos AS dc ON d.id_pag = dc.IdPago LEFT OUTER JOIN
                         dbo.DbDatos_Arrendamiento_Acum AS a ON d.rtl_id = a.Retailer AND d.id_pag = a.IdPago
WHERE        (d.estado = 0 and d.post_dat between  convert(varchar(6), @desde, 12) and convert(varchar(6), @hasta, 12))
GROUP BY d.rtl_id, d.id_pag, d.monto, dc.Monto, d.fecha_gen)a where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0) as debito_c
		, COALESCE((select sum(debitoArrendamiento) from (SELECT        d.rtl_id AS retailer, d.id_pag AS idPago, d.monto, COALESCE(dc.Monto, 0) AS debitoContracargos, COALESCE(SUM(a.Monto_Debitar), 0) AS debitoArrendamiento, 
                         CONVERT(date, d.fecha_gen) AS fecha
FROM            dbo.dftdet AS d LEFT OUTER JOIN
                         dbo.DbDatos_Debitos_Contracargos AS dc ON d.id_pag = dc.IdPago LEFT OUTER JOIN
                         dbo.DbDatos_Arrendamiento_Acum AS a ON d.rtl_id = a.Retailer AND d.id_pag = a.IdPago
WHERE        (d.estado = 0 and d.post_dat between  convert(varchar(6), @desde, 12) and convert(varchar(6), @hasta, 12))
GROUP BY d.rtl_id, d.id_pag, d.monto, dc.Monto, d.fecha_gen)a where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0) as debito_a
		, COALESCE((select max(monto) from (SELECT        d.rtl_id AS retailer, d.id_pag AS idPago, d.monto, COALESCE(dc.Monto, 0) AS debitoContracargos, COALESCE(SUM(a.Monto_Debitar), 0) AS debitoArrendamiento, 
                         CONVERT(date, d.fecha_gen) AS fecha
FROM            dbo.dftdet AS d LEFT OUTER JOIN
                         dbo.DbDatos_Debitos_Contracargos AS dc ON d.id_pag = dc.IdPago LEFT OUTER JOIN
                         dbo.DbDatos_Arrendamiento_Acum AS a ON d.rtl_id = a.Retailer AND d.id_pag = a.IdPago
WHERE        (d.estado = 0 and d.post_dat between  convert(varchar(6), @desde, 12) and convert(varchar(6), @hasta, 12))
GROUP BY d.rtl_id, d.id_pag, d.monto, dc.Monto, d.fecha_gen)a where retailer = c.retailer and fecha between convert(date, @Desde) and convert(date, @Hasta)), 0)  as maximo
	--from dbdatos.[dbo].[Vw_Arrendamiento_Pos_comercios] as c 

	from (SELECT        com.Retailer, CASE WHEN (com.consolidar = 2) THEN com.retailer ELSE com.rtlpadre END AS padre, com.Consolidar, com.Mto_Arrendamiento AS monto, 
                         CONVERT(date, DATEADD(month, - 1, com.Fecha_Creacion)) AS inicio, COALESCE (CONVERT(date, com.Fecha_Desabilitado), GETDATE()) AS retiro, 
                         (DATEDIFF(MONTH, CONVERT(date, DATEADD(month, - 1, com.Fecha_Creacion)), COALESCE(CONVERT(date, com.Fecha_Desabilitado), GETDATE())) - COALESCE(cob.cobros,
                          0)) * com.Mto_Arrendamiento AS saldo, DATEDIFF(MONTH, CONVERT(date, DATEADD(month, - 1, com.Fecha_Creacion)), COALESCE(CONVERT(date, 
                         com.Fecha_Desabilitado), GETDATE())) - COALESCE(cob.cobros, 0) AS debe, DATEADD(MONTH, COALESCE(cob.cobros, 0), CONVERT(date, DATEADD(month, - 1, 
                         com.Fecha_Creacion))) AS mes, com.No_Pos AS pos, com.Estado, com.Alertas
FROM            dbo.DbDatos_Arrendamiento_Comercios_Temp AS com LEFT OUTER JOIN
                        (SELECT        Retailer, COUNT(*) AS cobros
FROM            dbo.DbDatos_Arrendamiento_Acum
WHERE        (Estado = 1)
GROUP BY Retailer) cob ON com.Retailer = cob.RETAILER)c
LEFT  JOIN [dbdatos].[Comercios].[DbDatos_Bussiness_Data] b on c.Retailer = b.Retailer
	LEFT  JOIN [dbdatos].[Comercios].[DbDatos_Afiliation_Data] a on b.IdAfiliacion_Fk = a.Id
	LEFT  JOIN [dbdatos].[Instituciones].[DbDatos_Instituciones] i on a.IdInstitucion_Fk = i.Id
	WHERE 
			   (@Tipo = 1 AND c.debe < 2 AND c.estado = 1)
			OR (@Tipo = 2 AND c.debe > 1 AND c.estado = 1)
		ORDER BY
			c.debe
END 