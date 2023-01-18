using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Message("6001")]

public class GetMailList: MessageRequest
{

}


[Message("6002")]

public class GetMailDetail: MailMessageBase
{
 
}

[Message("6004")]

public class ClaimMail : MailMessageBase
{

}

[Message("6005")]

public class DeleteMail : MailMessageBase
{

}

[Message("6007")]

public class ClaimAllMail : MessageRequest
{

}

[Message("6008")]

public class DeleteAllMail : MessageRequest
{

}


public class MailMessageBase : MessageRequest
{
    public string MailId;
    public int Type;
}
