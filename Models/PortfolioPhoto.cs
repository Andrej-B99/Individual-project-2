using System;
using System.ComponentModel.DataAnnotations;

namespace MasterServicePlatform.Web.Models;

public class PortfolioPhoto
{
    public int Id { get; set; }

    public int MasterId { get; set; }
    public Master Master { get; set; }

    public string PhotoPath { get; set; }

    public DateTime UploadedAt { get; set; }
}
