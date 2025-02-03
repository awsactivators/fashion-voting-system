using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FashionVote.Models
{

  public class ParticipantShow
  {
      public int ParticipantId { get; set; }
      public Participant Participant { get; set; }

      public int ShowId { get; set; }
      public Show Show { get; set; }
  }

}
