using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using LoanApp.IServices;
using Microsoft.AspNetCore.Components;
using WordDocument;
using WordDocument.IServices;
using WordDocument.Model;
using LoanApp.Model.Helper;
using Microsoft.JSInterop;
using DocumentFormat.OpenXml.Packaging;
using LoanApp.DatabaseModel.LoanEntities;
using Paragraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using TableRow = DocumentFormat.OpenXml.Wordprocessing.TableRow;
using TableCell = DocumentFormat.OpenXml.Wordprocessing.TableCell;
using Tabs = DocumentFormat.OpenXml.Wordprocessing.Tabs;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;
using Table = DocumentFormat.OpenXml.Wordprocessing.Table;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using ParagraphProperties = DocumentFormat.OpenXml.Wordprocessing.ParagraphProperties;
using RunProperties = DocumentFormat.OpenXml.Wordprocessing.RunProperties;
using GridColumn = DocumentFormat.OpenXml.Wordprocessing.GridColumn;
using TableGrid = DocumentFormat.OpenXml.Wordprocessing.TableGrid;
using TableCellProperties = DocumentFormat.OpenXml.Wordprocessing.TableCellProperties;
using TabStop = DocumentFormat.OpenXml.Wordprocessing.TabStop;
using TableStyle = DocumentFormat.OpenXml.Wordprocessing.TableStyle;
using TableProperties = DocumentFormat.OpenXml.Wordprocessing.TableProperties;
using LoanApp.Services.Services;
using System.Linq;
using DocumentFormat.OpenXml.ExtendedProperties;

namespace LoanApp.Components.WordDoc
{
    public partial class EvidenceAfterLoanForm
    {
        [Parameter] public EventCallback<bool> IsLoading { get; set; }
        [Parameter] public string Size { get; set; } = AntDesign.ButtonSize.Default.ToString();
        [Parameter] public List<VLoanRequestContract> RequestContract { get; set; } = new();

        #region Inject
        [Inject] private LoanApp.Services.IServices.INotificationService notificationService { get; set; } = null!;

        [Inject] private IMSWordService mSWordService { get; set; } = null!;
        [Inject] private IWordOptions wordOptions { get; set; } = null!;
        [Inject] private LoanApp.Services.IServices.LoanDb.IPsuLoan psuLoan { get; set; } = null!;

        #endregion

        private async Task Export()
        {
            List<string> staffIdList = RequestContract
                .Where(c => !string.IsNullOrEmpty(c.DebtorStaffId))
                .Select(x => x.DebtorStaffId!)
                .Distinct()
                .ToList();

            if (staffIdList.Any())
            {
                await IsLoading.InvokeAsync(true);

                await Task.Delay(1);
                StateHasChanged();

                try
                {
                    using (var docBuilder = new WordDocumentBuilder())
                    {
                        string fileName = $"บันทึกข้อความติดตามหลักฐานการกู้เงิน-{dateService.ChangeDate(DateTime.Now, "dMMyyyy", Utility.DateLanguage_TH)}";
                        WordModel word = new();

                        List<PageOrientationWordModel> pageOrientations = new();
                        List<OpenXmlElement> paragraphs = new();

                        PageOrientationWordModel page = new();
                        PageMargin pageMargin = new();

                        foreach (var staffId in staffIdList)
                        {
                            List<VLoanRequestContract> requestContracts = RequestContract
                                .Where(c => c.DebtorStaffId == staffId)
                                .ToList();

                            #region DataDoc
                            pageMargin = new()
                            {
                                Top = wordOptions.InchToTwipReturnInt(1),
                                Bottom = wordOptions.InchToTwipReturnInt(0.19),
                                Left = wordOptions.InchToTwipReturnUint(1),
                                Right = wordOptions.InchToTwipReturnUint(0.82),
                            };

                            var dataDoc = await DataDoc(docBuilder, requestContracts);

                            paragraphs = new();
                            dataDoc.ForEach(x =>
                            {
                                paragraphs.Add(x);
                            });

                            #region Add paragraphs to Doc
                            page = new()
                            {
                                Body = new SdtBlock(wordOptions.SetNewSdtProperties(), wordOptions.SetNewSdtContentBlock(paragraphs)),
                                IsNewPage = true,
                                PageBodyMargin = pageMargin
                            };
                            #endregion
                            pageOrientations.Add(page);
                            #endregion
                        }

                        #region Table Dta
                        pageMargin = new()
                        {
                            Top = wordOptions.InchToTwipReturnInt(1),
                            Bottom = wordOptions.InchToTwipReturnInt(1),
                            Left = wordOptions.InchToTwipReturnUint(0.19),
                            Right = wordOptions.InchToTwipReturnUint(1),
                        };

                        var table = await TableDoc(RequestContract);

                        paragraphs = new();
                        table.ForEach(x =>
                        {
                            paragraphs.Add(x);
                        });

                        #region Add paragraphs to Doc
                        page = new()
                        {
                            Body = new SdtBlock(wordOptions.SetNewSdtProperties(), wordOptions.SetNewSdtContentBlock(paragraphs)),
                            IsNewPage = true,
                            Height = 11906U,
                            Width = 16838U,
                            Orient = PageOrientationValues.Landscape,
                            PageBodyMargin = pageMargin
                        };
                        #endregion
                        pageOrientations.Add(page);

                        #endregion

                        #region Set layout Doc
                        word.Bodys = pageOrientations;
                        word.PageDefaultMargin = new PageMargin()
                        {
                            Top = wordOptions.InchToTwipReturnInt(1),
                            Bottom = wordOptions.InchToTwipReturnInt(0.19),
                            Left = wordOptions.InchToTwipReturnUint(1),
                            Right = wordOptions.InchToTwipReturnUint(0.82),
                        };
                        word.PagrHeader = new() { Paragraphs = await SetHeaderDoc() };
                        word.PagrFooter = new() { Paragraphs = await SetHeaderDoc() };
                        #endregion

                        #region Gen Doc
                        bool isSucess = docBuilder.AddParagraph(word, wordOptions);

                        #endregion

                        #region Dowload Doc
                        if (isSucess)
                        {
                            using (var documentStream = docBuilder.Save())
                            {
                                byte[] documentBytes = documentStream.ToArray();

                                await JS.InvokeVoidAsync("jsSaveAsFile", $"{fileName}.docx", Convert.ToBase64String(documentBytes));
                            }
                        }
                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    _ = Task.Run(() => { notificationService.Error(notificationService.ExceptionLog(ex)); });
                }


                await IsLoading.InvokeAsync(false);
                await Task.Delay(1);
                StateHasChanged();
            }
        }

        private async Task<List<OpenXmlElement>> SetHeaderDoc()
        {
            List<OpenXmlElement> paragraphs = new();

            try
            {
                Paragraph paragraph = new(
                    new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Center },
                        new SpacingBetweenLines() { Before = "0", After = "0", }
                        ),
                    new Run(
                        new RunProperties(
                            new FontSize() { Val = wordOptions.FontPtToHpPt(28) },
                            new Color() { Val = "FF0000" }),
                        new Text("ลับ"))
                    );
                paragraphs.Add(paragraph);

            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return paragraphs;
        }


        private async Task<List<OpenXmlElement>> DataDoc(WordDocumentBuilder generator, List<VLoanRequestContract> requestContracts)
        {
            List<OpenXmlElement> paragraphs = new();

            try
            {
                List<OpenXmlElement> hearderImgs = await HeadderImg(generator);
                hearderImgs.ForEach(x =>
                {
                    paragraphs.Add(x);
                });

                List<OpenXmlElement> paragraphList = await ParagraphListData(requestContracts);
                paragraphList.ForEach(x =>
                {
                    paragraphs.Add(x);
                });

                List<OpenXmlElement> sign = await SignDoc();
                sign.ForEach(x =>
                {
                    paragraphs.Add(x);
                });

            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return paragraphs;
        }

        private async Task<List<OpenXmlElement>> HeadderImg(WordDocumentBuilder generator)
        {
            List<OpenXmlElement> paragraphs = new();
            Paragraph paragraph = new();

            try
            {
                #region image && บันทึกข้อความ
                paragraph = new();

                ParagraphProperties properties = new(
                    new SpacingBetweenLines()
                    {
                        After = "0"
                    },
                    new Justification()
                    {
                        Val = JustificationValues.Center
                    });

                var imgFileLocation = mSWordService.GetLocationFileDirImages("PSU-logo-Original.png");

                #region แก้ไขกรณีที่ไม่สามารถ อ่านไฟล์ภาพได้
                SaveFileAndImgService.AutoDeleteFileInFolderTemp();
                var newPathFile = Guid.NewGuid().ToString() + Path.GetExtension(imgFileLocation);
                var newImage = await SaveFileAndImgService.CopyFileAsync(imgFileLocation, newPathFile);

                #endregion

                ImgWordClassModel imgWord = generator.InsertImgToDoc(newImage, ImagePartType.Png);

                imgWord.PositionImg = PositionImgDocEnum.fix;
                imgWord.Cx = wordOptions.GenImageWidthAndHeiht(0.48);
                imgWord.Cy = wordOptions.GenImageWidthAndHeiht(0.8);

                Run run = generator.AddImageToCommandDoc(imgWord);

                paragraph.Append(new List<OpenXmlElement>() { properties });
                properties.Append(new List<OpenXmlElement>() { run });
                properties.Append(
                    new List<OpenXmlElement>()
                    {
                        new Run(
                            new RunProperties(
                                new FontSize()
                                {
                                    Val = $"{wordOptions.FontPtToHpPt(26)}"
                                },
                                new Bold(),
                                new BoldComplexScript()),
                            new Text()
                            {
                                Text = "บันทึกข้อความ"
                            })
                    });

                #endregion
                paragraphs.Add(paragraph);

                #region ส่วนงาน    
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Left,
                                    Position = 9000
                                }
                            })
                        ),
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new BoldComplexScript(),
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(18)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(18)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = "ส่วนงาน    "
                        }),
                    new Run(
                        new RunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = "งานสวัสดิการและสิทธิประโยชน์  กองบริหารทรัพยากรบุคคล     โทร. 2053-4"
                        }));

                #endregion
                paragraphs.Add(paragraph);

                #region ที่ && วันที่
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new TabStop()
                            {
                                Val = TabStopValues.Left,
                                Position = 4500
                            },
                            new TabStop()
                            {
                                Val = TabStopValues.Left,
                                Position = 9000
                            })),
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new BoldComplexScript(),
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(18)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(18)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = "ที่   "
                        }),
                    new Run(
                        new RunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = "มอ 003.4.4/ "
                        }),
                    new Run(
                        new RunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(19)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(19)}"
                            },
                            new ComplexScript()),
                        new TabChar()),
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new BoldComplexScript(),
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(18)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(18)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = "วันที่        "
                        }),
                    new Run(
                        new RunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = ""
                        }));

                #endregion
                paragraphs.Add(paragraph);

                #region เรื่อง    
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Left,
                                    Position = 9000
                                }
                            })
                        ),
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new BoldComplexScript(),
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(18)}"
                            },
                             new FontSizeComplexScript()
                             {
                                 Val = $"{wordOptions.FontPtToHpPt(18)}"
                             },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = "เรื่อง   "
                        }),
                    new Run(
                        new RunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Space = SpaceProcessingModeValues.Preserve,
                            Text = "การนำส่งหลักฐานหลังได้รับเงินกู้"
                        }));

                #endregion
                paragraphs.Add(paragraph);

            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return paragraphs;
        }

        private async Task<List<OpenXmlElement>> ParagraphListData(List<VLoanRequestContract> requestContracts)
        {
            List<OpenXmlElement> paragraphs = new();

            try
            {
                Paragraph paragraph = new();
                ParagraphProperties paragraphProperties = new();

                VLoanRequestContract data = requestContracts[0];
                VLoanStaffDetail? vLoanStaff = await psuLoan.GetUserDetailAsync(data.DebtorStaffId);

                #region เรียน
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            Before = "0",
                            After = "0"
                        },
                        new Indentation()
                        {
                            Left = "686",
                            Hanging = "686"
                        },
                        new Justification()
                        {
                            Val = JustificationValues.ThaiDistribute
                        }),
                    new Run(
                        new RunProperties(
                            new Bold(),
                            new BoldComplexScript(),
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Text = "เรียน"
                        }),
                    new Run(
                        new RunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new ComplexScript()),
                        new TabChar()),
                    new Run(
                        new RunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new ComplexScript()),
                        new Text()
                        {
                            Text = $"คุณ{data.DebtorNameTh} {data.DebtorSnameTh} {vLoanStaff?.FacNameThai}"
                        }));

                #endregion
                paragraphs.Add(paragraph);

                #region br
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new TabStop()
                        {
                            Val = TabStopValues.Left,
                            Position = 1134
                        },
                        new ParagraphMarkRunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(8)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(8)}"
                            })
                        ));

                #endregion
                paragraphs.Add(paragraph);

                #region body 1
                List<Run> runs = new();
                paragraph = new();
                paragraphProperties = new(
                    new Tabs(
                        new List<OpenXmlElement>()
                        {
                            new TabStop()
                            {
                                Val = TabStopValues.Left,
                                Position = 1440
                            }
                        }),
                    new Justification()
                    {
                        Val = JustificationValues.ThaiDistribute
                    },
                    new ParagraphMarkRunProperties(
                        new FontSize()
                        {
                            Val = $"{wordOptions.FontPtToHpPt(16)}"
                        },
                        new FontSizeComplexScript()
                        {
                            Val = $"{wordOptions.FontPtToHpPt(16)}"
                        })
                    );

                runs.Add(
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new TabChar()
                        })
                    );
                runs.Add(
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Space = SpaceProcessingModeValues.Preserve,
                                Text = "ตามที่ท่านได้ยื่นกู้เงินสวัสดิการของมหาวิทยาลัย "
                            }
                        })
                    );

                if (requestContracts.Any())
                {
                    for (int i = 0; i < requestContracts.Count; i++)
                    {
                        VLoanRequestContract item = requestContracts[i];

                        Run run = new(
                            new Text()
                            {
                                Space = SpaceProcessingModeValues.Preserve,
                                Text = $"ประเภท{item.LoanTypeName} " +
                                $"ตามสัญญาเลขที่ {item.ContractNo} " +
                                $"จำนวนเงิน {string.Format("{0:n2}", item.ContractLoanAmount)} บาท " +
                                $"ได้รับเงินกู้แล้วในวันที่ {dateService.ChangeDate(item.PaidDate, "d MMMMM yyyy", Utility.DateLanguage_TH)} "
                            });
                        runs.Add(run);

                        if ((i + 1) != requestContracts.Count)
                        {
                            runs.Add(
                                new Run(
                                    new List<OpenXmlElement>()
                                    {
                                        new Text()
                                        {
                                            Text = "และ"
                                        }
                                    })
                                );
                        }
                    }
                }

                runs.Add(
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Space = SpaceProcessingModeValues.Preserve,
                                Text = $"นั้น ด้วยประกาศมหาวิทยาลัยสงขลานครินทร์ เรื่อง สวัสดิการเงินกู้และเงินยืมบุคลากร มหาวิทยาลัยสงขลานครินทร์ ลง{Utility.LoanDocDate}"
                            }
                        })
                    );

                if (requestContracts.Any())
                {
                    List<byte?> loanTypeIds = requestContracts
                        .Where(c => (new List<byte?>() { 5, 6, 12, 13, 14 }).Contains(c.LoanTypeId))
                        .Select(x => x.LoanTypeId)
                        .Distinct()
                        .ToList();

                    if (loanTypeIds != null && loanTypeIds.Any())
                    {
                        runs.Add(
                            new Run(
                                new List<OpenXmlElement>()
                                {
                                    new Text()
                                    {
                                        Space = SpaceProcessingModeValues.Preserve,
                                        Text = "ประเภท "
                                    }
                                })
                            );

                        for (int i = 0; i < loanTypeIds.Count; i++)
                        {
                            string result = GetLoanTypeCase(loanTypeIds[i]);

                            if (!string.IsNullOrEmpty(result))
                            {
                                Run run = new(
                                    new Text()
                                    {
                                        Space = SpaceProcessingModeValues.Preserve,
                                        Text = $"{result} "
                                    });
                                runs.Add(run);

                                if ((i + 1) != loanTypeIds.Count)
                                {
                                    runs.Add(
                                        new Run(
                                            new List<OpenXmlElement>()
                                            {
                                                new Text()
                                                {
                                                    Space = SpaceProcessingModeValues.Preserve,
                                                    Text = "และ"
                                                }
                                            })
                                        );
                                }
                            }
                        }
                    }
                }

                runs.Add(
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Space = SpaceProcessingModeValues.Preserve,
                                Text = "ได้กำหนดให้ผู้กู้ยื่นใบเสร็จรับเงินหรือแสดงหลักฐานค่าใช้จ่ายให้ส่วนงาน " +
                                "ภายใน 30 วัน นับจากวันรับเงินกู้ " +
                                "หากผู้กู้ไม่ยื่นเอกสารดังกล่าว มหาวิทยาลัยจะเรียกเงินคืนทั้งจำนวน " +
                                "พร้อมคิดดอกเบี้ยร้อยละ 7.5 ต่อปี"
                            }
                        })
                    );

                paragraph.Append(new List<OpenXmlElement>() { paragraphProperties });
                runs.ForEach(run =>
                {
                    paragraph.Append(new List<OpenXmlElement>() { run });
                });

                #endregion
                paragraphs.Add(paragraph);

                #region body 2
                paragraph = new(
                    new ParagraphProperties(
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Left,
                                    Position = 900
                                }
                            }),
                        new Justification()
                        {
                            Val = JustificationValues.ThaiDistribute
                        },
                        new ParagraphMarkRunProperties(
                            new List<OpenXmlElement>()
                            {
                                new FontSizeComplexScript() { Val = $"{wordOptions.FontPtToHpPt(16)}" }
                            })
                        ),
                    new Run(new List<OpenXmlElement>() { new TabChar() }),
                    new Run(new List<OpenXmlElement>() { new TabChar() }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Space = SpaceProcessingModeValues.Preserve,
                                Text = $"ในการนี้งานสวัสดิการและสิทธิประโยชน์ กองบริหารทรัพยากรบุคคล มหาวิทยาลัยสงขลานครินทร์ ได้ตรวจสอบข้อมูลในระบบสวัสดิการเงินกู้ มหาวิทยาลัยสงขลานครินทร์ พบว่าท่านยังมิได้ยื่นใบเสร็จรับเงินและ/หรือแสดงหลักฐานค่าใช้จ่ายในระบบให้เรียบร้อย ซึ่งล่วงเลยระยะเวลาการยื่นเอกสารดังกล่าวแล้ว ดังนั้นจึงขอให้ท่านยื่นเอกสารให้ครบถ้วน โดยดำเนินการได้ที่ เว็บไซต์ "
                            }
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new RunProperties(
                                new List<OpenXmlElement>()
                                {
                                    new RunStyle(){ Val = "Hyperlink" }
                                }),
                            new Text()
                            {
                                Text = $"https://loan.psu.ac.th"
                            }
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Space = SpaceProcessingModeValues.Preserve,
                                Text = $" เลือกเมนูหลัก "
                            }
                        }),
                     new Run(
                        new List<OpenXmlElement>()
                        {
                            new RunProperties(
                                new Bold(),
                                new BoldComplexScript()),
                            new Text()
                            {
                                Space = SpaceProcessingModeValues.Preserve,
                                Text = $"“สัญญากู้ยืมเงิน”"
                            }
                        }),
                     new Run(
                         new List<OpenXmlElement>()
                         {
                             new Text()
                             {
                                 Space = SpaceProcessingModeValues.Preserve,
                                 Text = $" และกดปุ่ม "
                             }
                         }),
                     new Run(
                         new RunProperties(
                             new Bold(),
                             new BoldComplexScript()),
                         new Text()
                         {
                             Space = SpaceProcessingModeValues.Preserve,
                             Text = $"“อัปโหลดหลักฐาน”"
                         }),
                     new Run(
                         new List<OpenXmlElement>()
                         {
                             new Text()
                             {
                                 Space = SpaceProcessingModeValues.Preserve,
                                 Text = $" "
                             }
                         }),
                     new Run(
                         new RunProperties(
                             new List<OpenXmlElement>()
                             {
                                 new Underline(){ Val = UnderlineValues.Single }
                             }),
                         new Text()
                         {
                             Space = SpaceProcessingModeValues.Preserve,
                             Text = $"หากท่านไม่ดำเนินการให้แล้วเสร็จภายใน 15 วัน นับจากวันที่ท่านได้รับหนังสือฉบับนี้"
                         }),
                     new Run(
                         new List<OpenXmlElement>()
                         {
                             new Text()
                             {
                                 Space = SpaceProcessingModeValues.Preserve,
                                 Text = $" ทางมหาวิทยาลัยจะ"
                             }
                         }),
                     new Run(
                         new RunProperties(
                             new List<OpenXmlElement>()
                             {
                                 new Underline(){ Val = UnderlineValues.Single }
                             }),
                         new Text()
                         {
                             Space = SpaceProcessingModeValues.Preserve,
                             Text = $"เรียก เก็บเงินกู้คืนทั้งจำนวน พร้อมคิดดอกเบี้ยร้อยละ 7.5 ต่อปี"
                         }),
                      new Run(
                         new List<OpenXmlElement>()
                         {
                             new Text()
                             {
                                 Space = SpaceProcessingModeValues.Preserve,
                                 Text = $" ตามประกาศมหาวิทยาลัยสงขลานครินทร์ฯ ต่อไป"
                             }
                         })
                    );
                #endregion
                paragraphs.Add(paragraph);

                #region จึงเรียนมาเพื่อโปรดดำเนินการ
                paragraph = new(
                    new ParagraphProperties(
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Left,
                                    Position = 1440
                                }
                            }
                            ),
                        new Justification()
                        {
                            Val = JustificationValues.ThaiDistribute
                        },
                        new ParagraphMarkRunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            })
                        ),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new TabChar()
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Text = $"จึงเรียนมาเพื่อโปรดดำเนินการ"
                            }
                        })
                    );

                #endregion
                paragraphs.Add(paragraph);

            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return paragraphs;

        }

        private string GetLoanTypeCase(byte? loanTypeId)
        {
            switch (loanTypeId)
            {
                case 5:
                    return "5. เงินกู้เพื่อซื้อคอมพิวเตอร์และอุปกรณ์ IT";

                case 6:
                    return "6. สวัสดิการเงินกู้เพื่อส่งเสริมสุขภาวะบุคลากร/เครื่องมือออกกำลังกายเพื่อสุขภาพ";

                case 12:
                    return "8. เงินกู้เพื่อซื้อประกันชีวิตของบุคลากร คู่สมรสและบุตร";

                case 13:
                    return "9. เงินกู้เพื่อซื้อหรือซ่อมแซมยานพาหนะ";

                case 14:
                    return "10. เงินกู้เพื่อซ่อมแซมที่อยู่อาศัย";
            }
            return string.Empty;
        }

        private async Task<List<OpenXmlElement>> SignDoc()
        {
            List<OpenXmlElement> paragraphs = new();

            try
            {
                Paragraph paragraph = new();

                #region br
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new TabStop()
                        {
                            Val = TabStopValues.Left,
                            Position = 1134
                        }));

                #endregion
                paragraphs.Add(paragraph);

                #region br
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new TabStop()
                        {
                            Val = TabStopValues.Left,
                            Position = 1134
                        }));

                #endregion
                paragraphs.Add(paragraph);

                #region sg01
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Center,
                                    Position = 5580
                                }
                            }),
                        new ParagraphMarkRunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            })
                        ),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new TabChar()
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Text = "#sg01#"
                            }
                        })
                    );

                #endregion
                paragraphs.Add(paragraph);

                #region Name
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Center,
                                    Position = 5580
                                }
                            }),
                        new ParagraphMarkRunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            })
                        ),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new TabChar()
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Text = "(รองศาสตราจารย์ ดร.วศิน  สุวรรณรัตน์)"
                            }
                        })
                    );

                #endregion
                paragraphs.Add(paragraph);

                #region ตำแหน่ง
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Center,
                                    Position = 5580
                                }
                            }
                            ),
                        new ParagraphMarkRunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            })
                        ),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new TabChar()
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Text = "รองอธิการบดีวิทยาเขตหาดใหญ่"
                            }
                        })
                    );

                #endregion
                paragraphs.Add(paragraph);

                #region ประธานคณะอนุกรรมการสวัสดิการมหาวิทยาลัยสงขลานครินทร์
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Center,
                                    Position = 5580
                                }
                            }),
                        new ParagraphMarkRunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            })
                        ),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new TabChar()
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Text = "ประธานคณะอนุกรรมการสวัสดิการมหาวิทยาลัยสงขลานครินทร์"
                            }
                        })
                    );

                #endregion
                paragraphs.Add(paragraph);

                #region วิทยาเขตหาดใหญ่
                paragraph = new(
                    new ParagraphProperties(
                        new SpacingBetweenLines()
                        {
                            After = "0",
                            Before = "0"
                        },
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Center,
                                    Position = 5580
                                }
                            }),
                        new ParagraphMarkRunProperties(
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            })
                        ),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new TabChar()
                        }),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Text = "วิทยาเขตหาดใหญ่"
                            }
                        })
                    );

                #endregion
                paragraphs.Add(paragraph);

            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return paragraphs;
        }

        private async Task<List<OpenXmlElement>> TableDoc(List<VLoanRequestContract> requestContracts)
        {
            List<OpenXmlElement> paragraphs = new();

            try
            {
                Paragraph paragraph = new();
                //List<LoanType> loanTypes = await psuLoan.GetAllLoanType()

                #region header Text
                paragraph = new(
                    new ParagraphProperties(
                        new Tabs(
                            new List<OpenXmlElement>()
                            {
                                new TabStop()
                                {
                                    Val = TabStopValues.Left,
                                    Position = 9000
                                }
                            }),
                        new ParagraphMarkRunProperties(
                            new Bold(),
                            new BoldComplexScript(),
                            new FontSize()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            },
                            new FontSizeComplexScript()
                            {
                                Val = $"{wordOptions.FontPtToHpPt(16)}"
                            })
                        ),
                    new Run(
                        new List<OpenXmlElement>()
                        {
                            new Text()
                            {
                                Text = "รายการสัญญากู้ยืมเงิน"
                            }
                        })
                    );

                #endregion
                paragraphs.Add(paragraph);

                List<string> tableGridWidth = new() { "2016", "2124", "2971", "2548", "2549", "2549" };
                List<string> tableHearerName = new() { "ผู้กู้", "เลขที่สัญญา", "ประเภทกู้ยืม", "สถานะ", "ยอดเงินกู้", "วันที่ได้รับเงินกู้" };
                List<string> tableBodyData = new();

                foreach (var item in requestContracts)
                {
                    tableBodyData.Add("1");
                }


                paragraphs.Add(GenerateTable(tableGridWidth, tableHearerName, requestContracts));

            }
            catch (Exception ex)
            {
                await notificationService.ErrorDefult(notificationService.ExceptionLog(ex));
            }

            return paragraphs;
        }

        /// <summary>
        /// TableCellProperties
        /// </summary>
        /// <param name="width"></param>
        /// <returns></returns>
        private TableCellProperties SetTableCellPropertiesHeader(string width)
        {
            TableCellProperties tableCellProperties = new(
                    new TableCellWidth()
                    {
                        Width = width,
                        Type = TableWidthUnitValues.Dxa
                    });
            return tableCellProperties;
        }

        private ParagraphProperties SetParagraphPropertiesHeader()
        {
            ParagraphProperties paragraphProperties = new(
                    new Tabs(
                        new List<OpenXmlElement>()
                        {
                            new TabStop()
                            {
                                Val = TabStopValues.Left,
                                Position = 9000
                            }
                        }),
                    new Justification()
                    {
                        Val = JustificationValues.Center
                    },
                    new ParagraphMarkRunProperties(
                        new FontSize()
                        {
                            Val = $"{wordOptions.FontPtToHpPt(16)}"
                        },
                        new FontSizeComplexScript()
                        {
                            Val = $"{wordOptions.FontPtToHpPt(16)}"
                        }));

            return paragraphProperties;
        }

        public Table GenerateTable(List<string> gridColumn, List<string> hearerName, List<VLoanRequestContract> dataList)
        {
            Table table = new();
            TableGrid tableGrid1 = new();
            ParagraphProperties paragraphProperties = new();
            Run run = new();

            #region TableProperties
            TableProperties tableProperties = new(
                new TableStyle()
                {
                    Val = "TableGrid"
                },
                new TableWidth()
                {
                    Width = "14757",
                    Type = TableWidthUnitValues.Dxa
                },
                new TableLook()
                {
                    Val = "00A0",
                    FirstRow = true,
                    LastRow = false,
                    FirstColumn = true,
                    LastColumn = false,
                    NoHorizontalBand = false,
                    NoVerticalBand = false
                },
                new TableBorders(
                    new TopBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 4U
                    },
                    new BottomBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 4U
                    },
                    new LeftBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 4U
                    },
                    new RightBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 4U
                    },
                    new InsideHorizontalBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 4U
                    },
                    new InsideVerticalBorder()
                    {
                        Val = new EnumValue<BorderValues>(BorderValues.Single),
                        Size = 4U
                    })
                );

            #endregion

            foreach (var item in gridColumn)
            {
                tableGrid1.Append(
                    new List<OpenXmlElement>()
                    {
                        new GridColumn() { Width = item }
                    });
            }

            table.Append(new List<OpenXmlElement>() { tableProperties });
            table.Append(new List<OpenXmlElement>() { tableGrid1 });

            #region TableCell Header
            TableRow tableRowHearder = new();
            for (int i = 0; i < hearerName.Count; i++)
            {
                var item = hearerName[i];

                TableCell tableCell = new();
                Paragraph paragraph = new();

                run = new(
                    new RunProperties(
                        new Bold(),
                        new BoldComplexScript()),
                    new Text()
                    {
                        Text = item
                    });

                paragraph.Append(new List<OpenXmlElement>() { SetParagraphPropertiesHeader() });
                paragraph.Append(new List<OpenXmlElement>() { run });

                tableCell.Append(new List<OpenXmlElement>() { SetTableCellPropertiesHeader(gridColumn[i]) });
                tableCell.Append(new List<OpenXmlElement>() { paragraph });

                tableRowHearder.Append(new List<OpenXmlElement>() { tableCell });
            }

            table.Append(new List<OpenXmlElement>() { tableRowHearder });

            #endregion

            #region Table Body
            for (int i = 0; i < dataList.Count; i++)
            {
                var item = dataList[i];

                TableRow tableRowBody = new();

                #region Cell 1
                TableCell tableCell = new();
                Paragraph paragraph = new();
                TableCellProperties tableCellProperties = SetTableCellPropertiesHeader(gridColumn[0]);

                paragraphProperties = new(
                    new Justification()
                    {
                        Val = JustificationValues.Left
                    });

                run = new(
                    new Text()
                    {
                        Text = $"{item.DebtorNameTh} {item.DebtorSnameTh}"
                    });

                paragraph.Append(new List<OpenXmlElement>() { paragraphProperties });
                paragraph.Append(new List<OpenXmlElement>() { run });

                tableCell.Append(new List<OpenXmlElement>() { tableCellProperties });
                tableCell.Append(new List<OpenXmlElement>() { paragraph });

                tableRowBody.Append(new List<OpenXmlElement>() { tableCell });
                #endregion

                #region Cell 2
                tableCell = new();
                paragraph = new();
                tableCellProperties = SetTableCellPropertiesHeader(gridColumn[1]);

                paragraphProperties = new(
                    new Justification()
                    {
                        Val = JustificationValues.Center
                    });

                run = new(
                    new Text()
                    {
                        Text = $"{item.ContractNo}"
                    });

                paragraph.Append(new List<OpenXmlElement>() { paragraphProperties });
                paragraph.Append(new List<OpenXmlElement>() { run });

                tableCell.Append(new List<OpenXmlElement>() { tableCellProperties });
                tableCell.Append(new List<OpenXmlElement>() { paragraph });

                tableRowBody.Append(new List<OpenXmlElement>() { tableCell });
                #endregion

                #region Cell 3
                tableCell = new();
                paragraph = new();
                tableCellProperties = SetTableCellPropertiesHeader(gridColumn[2]);

                paragraphProperties = new(
                    new Justification()
                    {
                        Val = JustificationValues.Left
                    });

                run = new(
                    new Text()
                    {
                        Text = $"{item.LoanTypeName}"
                    });

                paragraph.Append(new List<OpenXmlElement>() { paragraphProperties });
                paragraph.Append(new List<OpenXmlElement>() { run });

                tableCell.Append(new List<OpenXmlElement>() { tableCellProperties });
                tableCell.Append(new List<OpenXmlElement>() { paragraph });

                tableRowBody.Append(new List<OpenXmlElement>() { tableCell });
                #endregion

                #region Cell 4
                tableCell = new();
                paragraph = new();
                tableCellProperties = SetTableCellPropertiesHeader(gridColumn[3]);

                paragraphProperties = new(
                    new Justification()
                    {
                        Val = JustificationValues.Left
                    });

                run = new(
                    new Text()
                    {
                        Text = $"{item.CurrentStatusName}"
                    });

                paragraph.Append(new List<OpenXmlElement>() { paragraphProperties });
                paragraph.Append(new List<OpenXmlElement>() { run });

                tableCell.Append(new List<OpenXmlElement>() { tableCellProperties });
                tableCell.Append(new List<OpenXmlElement>() { paragraph });

                tableRowBody.Append(new List<OpenXmlElement>() { tableCell });
                #endregion

                #region Cell 5
                tableCell = new();
                paragraph = new();
                tableCellProperties = SetTableCellPropertiesHeader(gridColumn[4]);

                paragraphProperties = new(
                    new Justification()
                    {
                        Val = JustificationValues.Center
                    });

                run = new(
                    new Text()
                    {
                        Text = $"{string.Format("{0:n2}", item.ContractLoanAmount)}"
                    });

                paragraph.Append(new List<OpenXmlElement>() { paragraphProperties });
                paragraph.Append(new List<OpenXmlElement>() { run });

                tableCell.Append(new List<OpenXmlElement>() { tableCellProperties });
                tableCell.Append(new List<OpenXmlElement>() { paragraph });

                tableRowBody.Append(new List<OpenXmlElement>() { tableCell });
                #endregion

                #region Cell 5
                tableCell = new();
                paragraph = new();
                tableCellProperties = SetTableCellPropertiesHeader(gridColumn[4]);

                paragraphProperties = new(
                    new Justification()
                    {
                        Val = JustificationValues.Left
                    });

                run = new(
                    new Text()
                    {
                        Text = $"{dateService.ChangeDate(item.PaidDate, "d MMMMM yyyy", Utility.DateLanguage_TH)}"
                    });

                paragraph.Append(new List<OpenXmlElement>() { paragraphProperties });
                paragraph.Append(new List<OpenXmlElement>() { run });

                tableCell.Append(new List<OpenXmlElement>() { tableCellProperties });
                tableCell.Append(new List<OpenXmlElement>() { paragraph });

                tableRowBody.Append(new List<OpenXmlElement>() { tableCell });
                #endregion

                table.Append(new List<OpenXmlElement>() { tableRowBody });
            }

            #endregion

            return table;
        }
    }
}
